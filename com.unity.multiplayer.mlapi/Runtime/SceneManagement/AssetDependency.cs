using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using System.Collections.ObjectModel;
using System.Collections.Specialized;
#endif

namespace MLAPI.SceneManagement
{
    [ExecuteInEditMode]
    [Serializable]
    public class AssetDependency : ScriptableObject, IAssetDependency
    {
#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        protected ObservableCollection<IAssetDependency> m_Dependencies = new ObservableCollection<IAssetDependency>();

        public void AddDependency(IAssetDependency assetDependency)
        {
            if(!m_Dependencies.Contains(assetDependency))
            {
                m_Dependencies.Add(assetDependency);
            }
        }
        public void RemoveDependency(IAssetDependency assetDependency)
        {
            if (m_Dependencies.Contains(assetDependency))
            {
                m_Dependencies.Remove(assetDependency);
            }
        }


        protected virtual void OnDependecyAdded(IAssetDependency dependencyAdded)
        {

        }

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
        private void Awake()
        {
#if UNITY_EDITOR
            m_Dependencies.CollectionChanged += Dependencies_CollectionChanged;
#endif
        }
    }

    public interface IAssetDependency
    {
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
