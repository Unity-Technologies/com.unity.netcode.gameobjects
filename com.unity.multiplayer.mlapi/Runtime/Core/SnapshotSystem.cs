using System;
using MLAPI.NetworkVariable;

namespace MLAPI
{
    public class SnapshotSystem : INetworkUpdateSystem, IDisposable
    {
        public SnapshotSystem()
        {
            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void Dispose()
        {
            this.UnregisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.EarlyUpdate:
                    break;
            }
        }

        public void Store(ulong networkObjectId, int index, INetworkVariable networkVariable)
        {

        }

    }
}
