using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.MonoBehaviours.Core;

namespace MLAPI.Data
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    public class NetworkedVar<T> : INetworkedVar
    {
        public bool IsDirty { get; set; }
        public readonly NetworkedVarSettings<T> Settings = new NetworkedVarSettings<T>();
        public float LastSyncedTime { get; internal set; }
        
        public delegate void OnValueChangedByRemoteDelegate(T newValue);
        public OnValueChangedByRemoteDelegate OnValueChangedByRemote;
        private NetworkedBehaviour networkedBehaviour;

        internal NetworkedVar()
        {
        }

        private T InternalValue;
        public T Value
        {
            get
            {
                return InternalValue;
            }
            set
            {
                if (Settings.SendOnChange)
                {
                    IsDirty = false;
                    LastSyncedTime = NetworkingManager.singleton.NetworkTime;
                    InternalValue = value;
                    //TODO: Send
                }
                else
                {
                    InternalValue = value;
                    IsDirty = true;
                }
            }
        }

        bool INetworkedVar.NeedsDirtySync()
        {
            if (!IsDirty && !Settings.SendOnChange && NetworkingManager.singleton.NetworkTime - LastSyncedTime >= Settings.SendDelay)
            {
                IsDirty = true;
            }
            return IsDirty;
        }

        bool INetworkedVar.CanClientRead(uint clientId)
        {
            switch (Settings.ReadPermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.ReadPermissionCallback == null) return false;
                    return Settings.ReadPermissionCallback(clientId);
                }
            }
            return true;
        }
        
        bool INetworkedVar.CanClientWrite(uint clientId)
        {
            switch (Settings.WritePermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.WritePermissionCallback == null) return false;
                    return Settings.WritePermissionCallback(clientId);
                }
            }

            return true;
        }

        void INetworkedVar.SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        void INetworkedVar.SetFieldFromReader(BitReader reader)
        {
            // TODO TwoTen - Boxing sucks
            T newValue = (T)FieldTypeHelper.ReadFieldType(reader, typeof(T), (object)InternalValue);
        }
        
        void INetworkedVar.WriteFieldToWriter(BitWriter writer)
        {
            //TODO: Write field
        }
    }

    internal interface INetworkedVar
    {
        bool NeedsDirtySync();
        bool CanClientWrite(uint clientId);
        bool CanClientRead(uint clientId);
        void WriteFieldToWriter(BitWriter writer);
        void SetFieldFromReader(BitReader reader);
        void SetNetworkedBehaviour(NetworkedBehaviour behaviour);
    }
}
