using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.ObjectModel;
using System.Collections.Specialized;
#endif
using MLAPI.Serialization;


namespace MLAPI.SceneManagement
{
    /// <summary>
    /// This class is designed primarily as an editor based helper class that will shed itself of all editor specific
    /// properties during runtime but will allow other classes to derive from it in order to maintain a list of asset
    /// dependencies.  Dependencies are ordered in a top down fashion such that any parents that depend upon any
    /// child-relative AssetDependency will notify the child-relative AssetDependency that it "depends upon" this asset.
    /// Notes:
    /// ExecuteInEditMode attribute is required in order to assure the ObservableCollection CollectionChanged event is 
    /// registered so changes in dependencies can be more easily
    /// managed.
    /// </summary>
    [ExecuteInEditMode]
    [Serializable]
    public class AssetDependency : ScriptableObject, IAssetDependency
    {
#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        protected ObservableCollection<IAssetDependency> m_Dependencies = new ObservableCollection<IAssetDependency>();

        /// <summary>
        /// Parent assets can use this to notify any child assets that it "depends" upon this asset
        /// </summary>
        /// <param name="assetDependency"></param>
        public void AddDependency(IAssetDependency assetDependency)
        {
            if(!m_Dependencies.Contains(assetDependency))
            {
                m_Dependencies.Add(assetDependency);
            }
        }

        /// <summary>
        /// Parent assets can use this to notify any child assets that it no longer "depends" upon this asset
        /// </summary>
        /// <param name="assetDependency"></param>
        public void RemoveDependency(IAssetDependency assetDependency)
        {
            if (m_Dependencies.Contains(assetDependency))
            {
                m_Dependencies.Remove(assetDependency);
            }
        }

        /// <summary>
        /// Child derived classes can override this method to recieve notifications of a 
        /// dependency addition 
        /// </summary>
        /// <param name="dependencyAdded"></param>
        protected virtual void OnDependecyAdded(IAssetDependency dependencyAdded)
        {

        }

        /// <summary>
        /// Child derived classes can override this method to recieve notifications of a 
        /// dependency removal 
        /// </summary>
        /// <param name="dependencyRemoved"></param>
        protected virtual void OnDependecyRemoved(IAssetDependency dependencyRemoved)
        {

        }

        private void DependencyRemoved(NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in e.OldItems)
            {
                OnDependecyRemoved(item as IAssetDependency);
            }
        }

        private void DependencyAdded(NotifyCollectionChangedEventArgs e)
        {
            foreach (var item in e.NewItems)
            {
                OnDependecyAdded(item as IAssetDependency);
            }
        }

        private void Dependencies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        DependencyAdded(e);
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        DependencyRemoved(e);
                        break;
                    }
            }
        }

        protected virtual bool OnIsRootAssetDependency()
        {
            return false;
        }

        public bool IsRootAssetDependency()
        {
            return OnIsRootAssetDependency();
        }

        protected virtual bool OnHasDependencies()
        {
            return (m_Dependencies.Count > 0);
        }

        public bool HasDependencies()
        {
            return OnHasDependencies();
        }

        public bool BelongsToRootAssetBranch()
        {
            if(m_Dependencies.Count == 0)
            {
                return IsRootAssetDependency();
            }
            foreach(var dependency in m_Dependencies)
            {
                if(dependency.BelongsToRootAssetBranch())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Virtual method to allow for customizing the logic that determines if
        /// an asset dependency should be included (defaults to true unless overridden
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnShouldAssetBeIncluded()
        {
            return true;
        }

        /// <summary>
        /// Determines if the asset has any dependencies that are marked to be included
        /// </summary>
        /// <returns>true or false</returns>
        public bool ShouldAssetBeIncluded()
        {
            if(IsRootAssetDependency())
            {
                return true;
            }
            if (HasDependencies())
            {
                foreach (var dependency in m_Dependencies)
                {
                    if (dependency.ShouldAssetBeIncluded())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

#endif

        protected virtual void OnWriteHashSynchValues(NetworkWriter writer)
        {

        }

        public void WriteHashSynchValues(NetworkWriter writer)
        {
            OnWriteHashSynchValues(writer);
        }

        private void Awake()
        {
#if UNITY_EDITOR
            m_Dependencies.CollectionChanged += Dependencies_CollectionChanged;
#endif
        }
    }

    public interface IAssetDependency
    {
        void WriteHashSynchValues(NetworkWriter writer);

#if UNITY_EDITOR
        void AddDependency(IAssetDependency assetDependency);
        void RemoveDependency(IAssetDependency assetDependency);

        bool HasDependencies();

        bool IsRootAssetDependency();

        bool BelongsToRootAssetBranch();

        bool ShouldAssetBeIncluded();

#endif
    }
}
