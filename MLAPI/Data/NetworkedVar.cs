using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.MonoBehaviours.Core;

namespace MLAPI.Data
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    public class NetworkedVar<T> : INetworkedVar
    {
        public bool isDirty { get; set; }
        public readonly NetworkedVarSettings Settings = new NetworkedVarSettings();
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
                isDirty = true;
                InternalValue = value;
            }
        }

        public void OnSynced()
        {
            isDirty = false;
            LastSyncedTime = NetworkingManager.singleton.NetworkTime;
        }

        public bool IsDirty()
        {
            if (!isDirty) return false;
            if (Settings.SendOnChange) return true;
            if (NetworkingManager.singleton.NetworkTime - LastSyncedTime >= Settings.SendDelay) return true;
            return isDirty;
        }

        public bool CanClientRead(uint clientId)
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

        public void WriteDeltaToWriter(BitWriter writer) => WriteFieldToWriter(writer); //The NetworkedVar is built for simple data types and has no delta.

        public bool CanClientWrite(uint clientId)
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

        public void SetDeltaFromReader(BitReader reader) => SetFieldFromReader(reader); //The NetworkedVar is built for simple data types and has no delta.

        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        public void SetFieldFromReader(BitReader reader)
        {
            // TODO TwoTen - Boxing sucks
            InternalValue = (T)FieldTypeHelper.ReadFieldType(reader, typeof(T), (object)InternalValue);
        }
        
        public void WriteFieldToWriter(BitWriter writer)
        {
            //TODO: Write field
        }
    }

    public interface INetworkedVar
    {
        void OnSynced();
        bool IsDirty();
        bool CanClientWrite(uint clientId);
        bool CanClientRead(uint clientId);
        void WriteDeltaToWriter(BitWriter writer);
        void WriteFieldToWriter(BitWriter writer);
        void SetFieldFromReader(BitReader reader);
        void SetDeltaFromReader(BitReader reader);
        void SetNetworkedBehaviour(NetworkedBehaviour behaviour);
    }
}
