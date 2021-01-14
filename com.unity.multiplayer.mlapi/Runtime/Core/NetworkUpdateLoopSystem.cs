using System;
using UnityEngine;


namespace MLAPI
{
    /// <summary>
    ///  NetworkUpdateLoopBehaviour
    ///  Derive from this class if you need to register a NetworkedBehaviour based class
    /// </summary>
    public class NetworkUpdateLoopBehaviour:NetworkedBehaviour, INetworkUpdateLoopSystem
    {
        protected virtual Action InternalRegisterNetworkUpdateStage(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return null;
        }

        public Action RegisterUpdate(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return InternalRegisterNetworkUpdateStage(stage);
        }

        protected void RegisterUpdateLoopSystem()
        {
            NetworkUpdateManager.NetworkLoopRegistration(this);
        }

        protected void OnNetworkLoopSystemRemove()
        {
            if(onNetworkLoopSystemDestroyed != null)
            {
                onNetworkLoopSystemDestroyed.Invoke(this);
            }
        }

        private Action<INetworkUpdateLoopSystem> onNetworkLoopSystemDestroyed;

        public void RegisterUpdateLoopSystemDestroyCallback(Action<INetworkUpdateLoopSystem> networkLoopSystemDestroyedCallback)
        {
            onNetworkLoopSystemDestroyed = networkLoopSystemDestroyedCallback;
        }
    }

    /// <summary>
    ///  UpdateLoopBehaviour
    ///  Derive from this class if you only require MonoBehaviour functionality
    /// </summary>
    public class UpdateLoopBehaviour:MonoBehaviour, INetworkUpdateLoopSystem
    {
        protected virtual Action InternalRegisterNetworkUpdateStage(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return null;
        }

        public Action RegisterUpdate(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return InternalRegisterNetworkUpdateStage(stage);
        }

        protected void RegisterUpdateLoopSystem()
        {
            NetworkUpdateManager.NetworkLoopRegistration(this);
        }

        protected void OnNetworkLoopSystemRemove()
        {
            if(onNetworkLoopSystemDestroyed != null)
            {
                onNetworkLoopSystemDestroyed.Invoke(this);
            }
        }

        private Action<INetworkUpdateLoopSystem> onNetworkLoopSystemDestroyed;

        public void RegisterUpdateLoopSystemDestroyCallback(Action<INetworkUpdateLoopSystem> networkLoopSystemDestroyedCallback)
        {
            onNetworkLoopSystemDestroyed = networkLoopSystemDestroyedCallback;
        }
    }

    /// <summary>
    /// GenericUpdateLoopSystem
    /// Derive from this class for generic (non-MonoBehaviour) classes
    /// </summary>
    public class GenericUpdateLoopSystem:INetworkUpdateLoopSystem
    {
        protected virtual Action InternalRegisterNetworkUpdateStage(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return null;
        }

        public Action RegisterUpdate(NetworkUpdateManager.NetworkUpdateStages stage )
        {
            return InternalRegisterNetworkUpdateStage(stage);
        }

        protected void RegisterUpdateLoopSystem()
        {
            NetworkUpdateManager.NetworkLoopRegistration(this);
        }

        protected void OnNetworkLoopSystemRemove()
        {
            if(onNetworkLoopSystemDestroyed != null)
            {
                onNetworkLoopSystemDestroyed.Invoke(this);
            }
        }

        private Action<INetworkUpdateLoopSystem> onNetworkLoopSystemDestroyed;

        public void RegisterUpdateLoopSystemDestroyCallback(Action<INetworkUpdateLoopSystem> networkLoopSystemDestroyedCallback)
        {
            onNetworkLoopSystemDestroyed = networkLoopSystemDestroyedCallback;
        }
    }


    /// <summary>
    /// INetworkUpdateLoopSystem
    /// Use this interface if you need a custom class beyond the scope of GenericUpdateLoopSystem, UpdateLoopBehaviour, and NetworkUpdateLoopBehaviour
    /// </summary>
    public interface INetworkUpdateLoopSystem
    {
        Action RegisterUpdate(NetworkUpdateManager.NetworkUpdateStages stage );

        void RegisterUpdateLoopSystemDestroyCallback(Action<INetworkUpdateLoopSystem>  networkLoopSystemDestroyedCallbsack);
    }
}
