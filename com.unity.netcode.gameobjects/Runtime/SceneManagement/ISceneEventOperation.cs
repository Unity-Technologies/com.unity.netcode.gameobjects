using System;
#if USING_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using AsyncOperation = UnityEngine.AsyncOperation;

namespace Unity.Netcode
{
    /// <summary>
    /// Abstraction over an asynchronous scene load/unload operation used by <see cref="SceneEventProgress"/>.
    /// This allows the scene event progress tracking to work with either a traditional
    /// <see cref="UnityEngine.AsyncOperation"/> (build-settings scenes) or an Addressables
    /// operation handle (Addressable scenes) without the rest of the system needing to know which.
    /// </summary>
    internal interface ISceneEventOperation
    {
        /// <summary>
        /// True once the underlying operation has completed.
        /// </summary>
        bool IsDone { get; }

        /// <summary>
        /// The completion percentage of the underlying operation (0.0 - 1.0).
        /// </summary>
        float PercentComplete { get; }

        /// <summary>
        /// Invoked when the underlying operation completes.
        /// </summary>
        event Action Completed;
    }

    /// <summary>
    /// <see cref="ISceneEventOperation"/> implementation that wraps a traditional
    /// <see cref="UnityEngine.AsyncOperation"/> (used for build-settings scenes).
    /// </summary>
    internal class EngineSceneOperation : ISceneEventOperation
    {
        private readonly AsyncOperation m_AsyncOperation;

        public bool IsDone => m_AsyncOperation.isDone;

        public float PercentComplete => m_AsyncOperation.progress;

        public event Action Completed;

        internal EngineSceneOperation(AsyncOperation asyncOperation)
        {
            m_AsyncOperation = asyncOperation;
            m_AsyncOperation.completed += _ => Completed?.Invoke();
        }
    }

#if USING_ADDRESSABLES
    /// <summary>
    /// <see cref="ISceneEventOperation"/> implementation that wraps an Addressables
    /// <see cref="AsyncOperationHandle{SceneInstance}"/> (used for Addressable scenes).
    /// </summary>
    internal class AddressableSceneOperation : ISceneEventOperation
    {
        private AsyncOperationHandle<SceneInstance> m_Handle;

        public bool IsDone => m_Handle.IsDone;

        public float PercentComplete => m_Handle.PercentComplete;

        public event Action Completed;

        internal AddressableSceneOperation(AsyncOperationHandle<SceneInstance> handle)
        {
            m_Handle = handle;
            m_Handle.Completed += _ => Completed?.Invoke();
        }
    }
#endif
}
