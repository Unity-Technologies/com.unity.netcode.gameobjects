using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MoverPickupObject : GenericMover
{

    protected override void OnStart()
    {
        base.OnStart();
    }

    public override void OnNetworkSpawn()
    {
        name += "-" + NetworkObjectId.ToString();
        if (!IsServer)
        {

        }

        base.OnNetworkSpawn();
    }

    public void SetHunter(ulong hunterId)
    {
        if (IsServer)
        {
            SetBeingHunted(hunterId);
        }
    }

    public ulong GetHunter()
    {
        return GetHunterObjectId();
    }

    public bool IsHunterHoldingObject(ulong clientId)
    {
        return GetHunterObjectId() == clientId && IsHunterHoldingObject();
    }

    /// <summary>
    /// Server Side:
    /// Drops the object
    /// </summary>
    public void DropObject()
    {
        if (IsServer)
        {
            var newPosition = transform.parent.transform.position + Vector3.right + Vector3.forward;
            StartCoroutine(NextPickupDelay(0.5f));
            transform.parent = null;
            transform.position = newPosition;
            m_LocalCollider.enabled = true;
            m_RigidBody.isKinematic = false;
            MovementEnabled.Value = true;
            MoveObjectBackToOriginalSceneClientRpc();
        }
    }

    [ClientRpc]
    public void MoveObjectBackToOriginalSceneClientRpc()
    {
        if (!IsServer && m_ParentSpawnHandler == null)
        {
            m_ParentSpawnHandler = FindObjectOfType<ParentingSpawnHandler>();
        }

        // Check to see if we are in our original scene and if not then move us back into that scene if it is still valid
        if (m_ParentSpawnHandler != null)
        {
            SceneManager.MoveGameObjectToScene(gameObject, m_ParentSpawnHandler.GetMyTargetScene(this));
        }
    }

    // The scene we were originally located within
    private ParentingSpawnHandler m_ParentSpawnHandler;

    public void SetParentSpawnHandler(ParentingSpawnHandler parentingSpawnHandler)
    {
        m_ParentSpawnHandler = parentingSpawnHandler;
        MoveObjectBackToOriginalSceneClientRpc();
    }


    protected override void HandleCollision(Collider collider)
    {
        base.HandleCollision(collider);

        if (NetworkManager != null && NetworkManager.IsListening && IsServer)
        {
            if (collider.CompareTag("Floor") || collider.CompareTag("Boundary"))
            {
                return;
            }

            var networkObjectOther = collider.gameObject.GetComponent<NetworkObject>();

            if (networkObjectOther != null && networkObjectOther.IsPlayerObject)
            {
                // First see if we can be picked up
                if (!CanBePickedUp(networkObjectOther.OwnerClientId))
                {
                    // If not then bail early
                    return;
                }
                else // Double check to make sure we don't have a parent
                if (transform.parent != null)
                {
                    Debug.LogWarning($"{name} thinks it can be picked up but still has a parent!");
                }

                // If there is already someone hunting it, then don't pick it up
                if (GetHunterObjectId() == networkObjectOther.OwnerClientId)
                {
                    if (NetworkObject.TrySetParent(networkObjectOther.gameObject))
                    {
                        SetPickedUp();
                        MovementEnabled.Value = false;
                        m_LocalCollider.enabled = false;
                        m_RigidBody.isKinematic = true;
                        transform.position = networkObjectOther.transform.position + Vector3.up;
                    }
                    else
                    {
                        Debug.LogError($"Client {networkObjectOther.OwnerClientId} failed to pickup {name}!");
                    }
                }
            }
        }
    }

    /// <summary>
    /// All Parenting State Related Game Logic is From Here Down
    /// </summary>
    private NetworkVariable<bool> m_DelayUntilNextPickup = new NetworkVariable<bool>();
    private NetworkVariable<bool> m_HunterHoldingObject = new NetworkVariable<bool>();

    private NetworkVariable<bool> m_IsBeingHunted = new NetworkVariable<bool>();
    private NetworkVariable<short> m_HunterObjectId = new NetworkVariable<short>();


    public void SetPickedUp()
    {
        m_HunterHoldingObject.Value = true;
    }

    public bool IsHunterHoldingObject()
    {
        return m_HunterHoldingObject.Value;
    }


    public ulong GetHunterObjectId()
    {
        return (ulong)m_HunterObjectId.Value;
    }

    public void SetBeingHunted(ulong hunterObjectId)
    {
        m_HunterObjectId.Value = (short)hunterObjectId;
        m_IsBeingHunted.Value = true;
    }

    public bool CanBePickedUp(ulong hunterObjectId)
    {
        if (m_IsBeingHunted.Value)
        {
            return (hunterObjectId == (ulong)m_HunterObjectId.Value) && !m_DelayUntilNextPickup.Value;
        }

        return !m_DelayUntilNextPickup.Value;
    }

    /// <summary>
    /// Resets the pickup object and delays for a bit until it can be picked up again to avoid
    /// immediate "pickup" when dropped.
    /// </summary>
    /// <param name="pickupDelay">how long to wait until it can be picked up</param>
    /// <returns></returns>
    public IEnumerator NextPickupDelay(float pickupDelay)
    {
        m_IsBeingHunted.Value = false;
        m_HunterHoldingObject.Value = false;
        m_DelayUntilNextPickup.Value = true;
        m_HunterObjectId.Value = -1;
        yield return new WaitForSeconds(pickupDelay);
        m_DelayUntilNextPickup.Value = false;
        yield return null;
    }
}

public struct PickupObjectState : INetworkSerializable
{
    private bool m_DelayUntilNextPickup;
    private bool m_HunterHoldingObject;
    private bool m_IsBeingHunted;
    private short m_HunterObjectId;

    public bool IsDirty { get; internal set; }

    public void NetworkSerialize(NetworkSerializer serializer)
    {
        if (!serializer.IsReading)
        {
            serializer.Writer.WriteBool(m_DelayUntilNextPickup);
            serializer.Writer.WriteBool(m_HunterHoldingObject);
            serializer.Writer.WriteBool(m_IsBeingHunted);
            serializer.Writer.WriteInt16Packed(m_HunterObjectId);
        }
        else
        {
            m_DelayUntilNextPickup = serializer.Reader.ReadBool();
            m_HunterHoldingObject = serializer.Reader.ReadBool();
            m_IsBeingHunted = serializer.Reader.ReadBool();
            m_HunterObjectId = serializer.Reader.ReadInt16Packed();
        }
        IsDirty = false;
    }

    public void SetPickedUp()
    {
        m_HunterHoldingObject = true;
        IsDirty = true;
    }

    public bool IsHunterHoldingObject()
    {
        return m_HunterHoldingObject;
    }


    public ulong GetHunterObjectId()
    {
        return (ulong)m_HunterObjectId;
    }

    public void SetBeingHunted(ulong hunterObjectId)
    {
        m_HunterObjectId = (short)hunterObjectId;
        m_IsBeingHunted = true;
        IsDirty = true;
    }

    public bool CanBePickedUp(ulong hunterObjectId)
    {
        if (m_IsBeingHunted )
        {
            return (hunterObjectId == (ulong)m_HunterObjectId) && !m_DelayUntilNextPickup;
        }

        return true;
    }

    public IEnumerator NextPickupDelay(float pickupDelay)
    {
        m_IsBeingHunted = false;
        m_HunterHoldingObject = false;
        m_DelayUntilNextPickup = true;
        m_HunterObjectId = -1;
        IsDirty = true;
        yield return new WaitForSeconds(pickupDelay);
        m_DelayUntilNextPickup = false;
        yield return null;
    }
}
