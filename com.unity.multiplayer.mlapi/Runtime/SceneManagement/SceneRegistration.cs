using System;
using System.Linq;
using System.Collections.Generic;
using MLAPI.Serialization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
#endif

namespace MLAPI.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneRegistration", menuName = "MLAPI/SceneManagement/SceneRegistration")]
    [Serializable]
    public class SceneRegistration : AssetDependency
    {
        [SerializeField]
        internal List<SceneEntry> SceneRegistrations;

        [HideInInspector]
        [SerializeField]
        private string m_NetworkManagerScene;

#if UNITY_EDITOR
        [HideInInspector]
        [SerializeField]
        private List<SceneEntry> m_KnownSceneRegistrations;

        #region Static Methods and Properties

        public static string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        private static void BuildLookupTableFromEditorBuildSettings()
        {
            foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
            {
                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(editorBuildSettingsScene.path))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(editorBuildSettingsScene.path, editorBuildSettingsScene);
                }
            }
        }

        /// <summary>
        /// This is needed in the event you have multiple assembly definitions that all have SceneRegistrations within them
        /// We have to make sure that we use the scene path as the key in order to allow for "the same scene name" to exist
        /// but in a different path.  As such, when we are synchronizing our build settings scenes in build list we need to
        /// always do a full comparison against the existing scenes in build list and our current assembly's scenes in build
        /// list.
        /// </summary>
        /// <param name="removeEntry">path to sceneAsset that will be excluding from build settings scenes in build list</param>
        internal static void SynchronizeScenes(string removeEntry = null)
        {
            var currentScenes = new Dictionary<string, EditorBuildSettingsScene>();
            foreach (var sceneEntry in EditorBuildSettings.scenes)
            {
                if (removeEntry != null && sceneEntry.path == removeEntry)
                {
                    continue;
                }
                if (!currentScenes.ContainsKey(sceneEntry.path))
                {
                    currentScenes.Add(sceneEntry.path, sceneEntry);
                }
                else
                {
                    Debug.LogWarning($"{sceneEntry.path} already exists in dictionary!");
                }
            }

            foreach (var keyPair in NetworkManager.BuildSettingsSceneLookUpTable)
            {
                if (!currentScenes.ContainsKey(keyPair.Key))
                {
                    currentScenes.Add(keyPair.Key, keyPair.Value);
                }
            }
            currentScenes = currentScenes.OrderBy(x => x.Key).ToDictionary((keyItem) => keyItem.Key, (valueItem) => valueItem.Value);

            EditorBuildSettings.scenes = currentScenes.Values.ToArray();
        }

        /// <summary>
        /// Adds or removes a scene asset to the build settings scenes in build list
        /// </summary>
        /// <param name="scene">SceneAsset</param>
        /// <param name="addScene">true or false</param>
        internal static void AddOrRemoveSceneAsset(SceneAsset scene, bool addScene)
        {
            if (NetworkManager.BuildSettingsSceneLookUpTable == null)
            {
                NetworkManager.BuildSettingsSceneLookUpTable = new Dictionary<string, EditorBuildSettingsScene>();
            }

            if (NetworkManager.BuildSettingsSceneLookUpTable.Count != EditorBuildSettings.scenes.Length)
            {
                BuildLookupTableFromEditorBuildSettings();
            }

            var assetPath = AssetDatabase.GetAssetPath(scene);

            if (addScene)
            {
                // If the scene does not exist in our local list, then add it and update the build settings
                if (!NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(assetPath))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Add(assetPath, new EditorBuildSettingsScene(assetPath, true));
                    SynchronizeScenes();
                }
            }
            else
            {
                // If the scene does exist in our local list, then remove it
                if (NetworkManager.BuildSettingsSceneLookUpTable.ContainsKey(assetPath))
                {
                    NetworkManager.BuildSettingsSceneLookUpTable.Remove(assetPath);
                    SynchronizeScenes(assetPath);
                }
            }
        }
        #endregion

        internal bool AssignedToNetworkManager
        {
            get
            {
                if (NetworkManagerScene != null)
                {
                    return true;
                }
                return false;
            }
        }

        [SceneReadOnlyProperty]
        [SerializeField]
        internal SceneAsset NetworkManagerScene;

        internal void AssignNetworkManagerScene(bool isAssigned = true)
        {
            if (isAssigned)
            {
                var currentScene = SceneManager.GetActiveScene();
                NetworkManagerScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);
                if (NetworkManagerScene != null)
                {
                    m_NetworkManagerScene = NetworkManagerScene.name;
                    UpdateNetworkManagerSceneEntry(NetworkManagerScene, true);
                    //AddOrRemoveSceneAsset(NetworkManagerScene, true);
                }
            }
            else
            {
                if (NetworkManagerScene != null)
                {
                    UpdateNetworkManagerSceneEntry(NetworkManagerScene, false);
                    //AddOrRemoveSceneAsset(NetworkManagerScene, false);
                }
                NetworkManagerScene = null;
                m_NetworkManagerScene = string.Empty;
            }
            ValidateBuildSettingsScenes();
        }

        private SceneEntry GetAssociatedSceneEntry(SceneAsset sceneAsset)
        {
            foreach (var sceneEntry in SceneRegistrations)
            {
                if (sceneEntry != null && sceneEntry.Scene == sceneAsset)
                {
                    return sceneEntry;
                }
            }

            return null;
        }


        private void UpdateNetworkManagerSceneEntry(SceneAsset sceneAsset, bool isAdding)
        {
            if (SceneRegistrations == null)
            {
                SceneRegistrations = new List<SceneEntry>();
            }

            var sceneEntry = GetAssociatedSceneEntry(sceneAsset);


            if(isAdding && sceneEntry == null)
            {
                var newEntry = new SceneEntry();
                newEntry.IncludeInBuild = true;
                newEntry.Scene = sceneAsset;
                newEntry.IsNetworkManagerScene = true;
                SceneRegistrations.Insert(0,newEntry);      //Always make it the zero slot index
            }
            else if (!isAdding && sceneEntry != null)
            {
                SceneRegistrations.Remove(sceneEntry);
            }
        }

        /// <summary>
        /// For this asset dependency, we check to see if we have been added to a NetworkManager instance within a scene
        /// that is contained within the project
        /// </summary>
        /// <returns>true or false</returns>
        protected override bool OnShouldAssetBeIncluded()
        {
            return AssignedToNetworkManager;
        }

        private bool m_HasInitialized;

        private void OnValidate()
        {
            ValidateBuildSettingsScenes();
        }

        /// <summary>
        /// Called to determine if there needs to be any adjustments to the build settings
        /// scenes in build list.
        /// </summary>
        internal void ValidateBuildSettingsScenes()
        {
            //NSS TODO: Temporary hack to just assure we receive these messages
            EditorSceneManager.sceneClosing -= EditorSceneManager_sceneClosing;
            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosing += EditorSceneManager_sceneClosing;
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            //Cycle through all scenes registered and validate the build settings scenes list
            if (SceneRegistrations != null && SceneRegistrations.Count > 0)
            {
                var shouldInclude = ShouldAssetBeIncluded();
                var partOfRootBranch = BelongsToRootAssetBranch();
                foreach (var sceneEntry in SceneRegistrations)
                {
                    if (sceneEntry != null && sceneEntry.Scene != null)
                    {
                        sceneEntry.AddDependency(this);
                        AddOrRemoveSceneAsset(sceneEntry.Scene, shouldInclude && partOfRootBranch && sceneEntry.IncludeInBuild);
                        sceneEntry.UpdateAdditiveSceneGroup();
                    }
                }
            }

            if (NetworkManagerScene != null)
            {
                AddOrRemoveSceneAsset(NetworkManagerScene, true);
            }

        }

        /// <summary>
        /// This is the root deciding factor for all checks to determine if assets referenced
        /// within this specific branch of scene asset references should be included.
        /// Note: if there are other SceneRegistration instances assigned to other NetworkManagers
        /// then all or some (depending upon what is included in the other SceneRegistration branches)
        /// will still be included.
        /// </summary>
        /// <returns></returns>
        protected override bool OnIsRootAssetDependency()
        {
            return OnShouldAssetBeIncluded();
        }


        private SceneEntry GetSceneEntryFromScene(Scene scene)
        {
            foreach (var sceneEntry in SceneRegistrations)
            {
                if (sceneEntry != null && sceneEntry.Scene != null)
                {
                    var sceneEntryPath = AssetDatabase.GetAssetPath(sceneEntry.Scene);
                    if(sceneEntryPath == scene.path)
                    {
                        return sceneEntry;
                    }
                }
            }
            return null;
        }

        private bool AreAnySceneEntriesCurrentlyLoaded()
        {
            return (GetCurrentlyOpenedSceneEntry() != null);
        }

        private SceneEntry GetCurrentlyOpenedSceneEntry()
        {
            foreach (var sceneEntry in SceneRegistrations)
            {
                if (sceneEntry != null && sceneEntry.IsCurrentlyOpened)
                {
                    return sceneEntry;
                }
            }
            return null;
        }


        internal void RefreshCurrentlyOpenedSceneEntry()
        {
            var openedSceneEntry = GetCurrentlyOpenedSceneEntry();
            if (openedSceneEntry != null)
            {
                openedSceneEntry.RefreshAdditiveScenes();
            }
        }


        private void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            var sceneEntry = GetSceneEntryFromScene(scene);
            if (sceneEntry != null)
            {
                // Circular reference check:
                // A base (master) scene of a SceneEntry cannot reference another base scene that belongs to
                // a SceneEntry of the same SceneRegistration.
                if(!AreAnySceneEntriesCurrentlyLoaded())
                {
                    sceneEntry.BaseSceneLoaded();
                }
                else
                {
                    Debug.LogError($"Detected SceneEntry {scene.name} being added to another base(master) scene from another SceneEntry!  Circular references are not allowed!");
                    sceneEntry.IsCurrentlyOpened = false;
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            else
            {
                RefreshCurrentlyOpenedSceneEntry();
            }
        }

        /// <summary>
        /// Trap to detect if there were any updates made to the scene hierarchy
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="removingScene"></param>
        private void EditorSceneManager_sceneClosing(Scene scene, bool removingScene)
        {
            var sceneEntry = GetSceneEntryFromScene(scene);
            if (sceneEntry != null)
            {
                sceneEntry.BaseSceneUnloadeding();
            }
        }
#endif

        /// <summary>
        /// Invoked to generate the hash value for the NetworkConfig comparison when a client is connecting
        /// </summary>
        /// <param name="writer"></param>
        protected override void OnWriteHashSynchValues(NetworkWriter writer)
        {
            if (m_NetworkManagerScene != null && m_NetworkManagerScene != string.Empty)
            {
                writer.WriteString(m_NetworkManagerScene);
            }

            foreach (var sceneRegistrationEntry in SceneRegistrations)
            {
                if (sceneRegistrationEntry != null && sceneRegistrationEntry.SceneEntryName != null && sceneRegistrationEntry.SceneEntryName != string.Empty)
                {
                    writer.WriteString(sceneRegistrationEntry.SceneEntryName);
                    if (sceneRegistrationEntry.AdditiveSceneGroup != null)
                    {
                        sceneRegistrationEntry.AdditiveSceneGroup.WriteHashSynchValues(writer);
                    }
                }
            }
        }

        /// <summary>
        /// Gets all scene names within this scene registration's scope
        /// NOTE: Scene names can be the same and this is not a good way to distinguish between scenes but is
        /// used for backwards compatibility purposes until no longer needed
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllScenes()
        {
            var allScenes = new List<string>();

            if (m_NetworkManagerScene != null && m_NetworkManagerScene != string.Empty)
            {
                allScenes.Add(m_NetworkManagerScene);
            }

            foreach (var sceneRegistrationEntry in SceneRegistrations)
            {
                if (sceneRegistrationEntry != null && sceneRegistrationEntry.SceneEntryName != null && sceneRegistrationEntry.SceneEntryName != string.Empty)
                {
                    allScenes.Add(sceneRegistrationEntry.SceneEntryName);
                }
            }

            return allScenes;
        }
    }

#if UNITY_EDITOR
    [Serializable]
    public class NetworkSceneSetup:SceneSetup
    {


        public SceneAsset SceneAsset;
    }

#endif


    /// <summary>
    /// A container class to hold the editor specific assets and
    /// the scene name that it is pointing to for runtime
    /// </summary>
    [Serializable]
    public class SceneEntry : SceneEntryBsase
    {

        public AdditiveSceneGroup AdditiveSceneGroup;

#if UNITY_EDITOR

        internal bool IsCurrentlyOpened;

        [SerializeField]
        internal LoadSceneMode Mode;

        [ReadOnlyProperty]
        [SerializeField]
        internal List<SceneSetup> m_SavedSceneSetup;

        [HideInInspector]
        [SerializeField]
        private Dictionary<SceneSetup, SceneAsset> m_SceneSetupToSceneAssetTable = new Dictionary<SceneSetup, SceneAsset>();

        [HideInInspector]
        [SerializeField]
        internal bool IsNetworkManagerScene;

        [HideInInspector]
        [SerializeField]
        internal AdditiveSceneGroup KnownAdditiveSceneGroup;


        private void ValidateSceneSetupToSceneAssetTable()
        {
            foreach(var keyPair in m_SceneSetupToSceneAssetTable)
            {
                var sceneAssetPath = AssetDatabase.GetAssetPath(keyPair.Value);
                if(keyPair.Key.path != sceneAssetPath)
                {
                    Debug.LogWarning($"Detected SceneAsset name or path change from {keyPair.Key.path} to {sceneAssetPath}");
                    keyPair.Key.path = sceneAssetPath;
                }
            }
        }

        internal void BaseSceneLoaded()
        {
            IsCurrentlyOpened = true;
            ValidateSceneSetupToSceneAssetTable();
            var currentSceneSetup = EditorSceneManager.GetSceneManagerSetup();
            if (Scene != null)
            {
                var sceneEntryPath = AssetDatabase.GetAssetPath(Scene);
                foreach (var sceneSetup in m_SavedSceneSetup)
                {
                    if (sceneEntryPath != sceneSetup.path)
                    {
                        if (!currentSceneSetup.Contains(sceneSetup))
                        {
                            if (sceneSetup.isLoaded)
                            {
                                EditorSceneManager.OpenScene(sceneSetup.path, OpenSceneMode.Additive);
                            }
                            else
                            {
                                EditorSceneManager.OpenScene(sceneSetup.path, OpenSceneMode.AdditiveWithoutLoading);
                            }
                        }
                    }
                }
            }
        }

        private void RebuildSceneSetupToSceneAssetTable()
        {
            m_SceneSetupToSceneAssetTable = new Dictionary<SceneSetup, SceneAsset>();
            foreach(var sceneSetup in m_SavedSceneSetup)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneSetup.path);
                m_SceneSetupToSceneAssetTable.Add(sceneSetup, sceneAsset);
            }
        }

        internal void BaseSceneUnloadeding()
        {
            if (IsCurrentlyOpened)
            {
                IsCurrentlyOpened = false;
                RefreshAdditiveScenes();
            }
        }

        internal void RefreshAdditiveScenes()
        {
            m_SavedSceneSetup = new List<SceneSetup>(EditorSceneManager.GetSceneManagerSetup());
            RebuildSceneSetupToSceneAssetTable();
        }

        internal void UpdateAdditiveScenes()
        {

        }

        /// <summary>
        /// Updates the dependencies for the additive scene group associated with this SceneEntry
        /// </summary>
        /// <param name="assetDependency"></param>
        internal void UpdateAdditiveSceneGroup()
        {
            if (AdditiveSceneGroup != KnownAdditiveSceneGroup)
            {
                if (KnownAdditiveSceneGroup != null)
                {
                    KnownAdditiveSceneGroup.RemoveDependency(this);
                    KnownAdditiveSceneGroup.ValidateBuildSettingsScenes();
                }
            }

            if (AdditiveSceneGroup != null)
            {
                if (IncludeInBuild)
                {
                    AdditiveSceneGroup.AddDependency(this);
                }
                else
                {
                    AdditiveSceneGroup.RemoveDependency(this);
                }
                AdditiveSceneGroup.ValidateBuildSettingsScenes();
            }

            if (KnownAdditiveSceneGroup != AdditiveSceneGroup)
            {
                KnownAdditiveSceneGroup = AdditiveSceneGroup;
            }
        }

        protected override void OnAddedToList()
        {

            base.OnAddedToList();
        }

        protected override void OnRemovedFromList()
        {
            if (!IsNetworkManagerScene)
            {
                IncludeInBuild = false;
                if (Scene != null)
                {
                    SceneRegistration.AddOrRemoveSceneAsset(Scene, false);
                }
                UpdateAdditiveSceneGroup();
            }
        }

#endif

    }


    /// <summary>
    /// A container class to hold the editor specific assets and
    /// the scene name that it is pointing to for runtime
    /// </summary>
    [Serializable]
    public class SceneEntryBsase : ISerializationCallbackReceiver, ISmartItem, IAssetDependency
    {
#if UNITY_EDITOR
        [Tooltip("When set to true, this will automatically register all of the additive scenes (including groups) with the build settings scenes in build list.  If false, then the scene(s) have to be manually added or will not be included in the build.")]
        public bool IncludeInBuild;
        public SceneAsset Scene;
#endif
        [HideInInspector]
        public string SceneEntryName;

        public void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// This is used to extract the scene name from the SceneAsset
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (Scene != null && SceneEntryName != Scene.name)
            {
                SceneEntryName = Scene.name;
            }
#endif
        }

        protected virtual void OnAddedToList()
        {

        }

        public void AddedToList()
        {
#if UNITY_EDITOR
            m_Dependencies.CollectionChanged += Dependencies_CollectionChanged;
#endif
            OnAddedToList();
        }

        protected virtual void OnRemovedFromList()
        {

        }

        public void RemovedFromList()
        {
            OnRemovedFromList();
#if UNITY_EDITOR
            m_Dependencies.CollectionChanged -= Dependencies_CollectionChanged;
#endif
        }


        protected virtual void OnWriteHashSynchValues(NetworkWriter writer)
        {
            if (SceneEntryName != null && SceneEntryName != string.Empty)
            {
                writer.WriteString(SceneEntryName);
            }
        }

        public void WriteHashSynchValues(NetworkWriter writer)
        {
            OnWriteHashSynchValues(writer);
        }


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
            if (!m_Dependencies.Contains(assetDependency))
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
        /// Child derived classes can override this method to receive notifications of a
        /// dependency addition
        /// </summary>
        /// <param name="dependencyAdded"></param>
        protected virtual void OnDependecyAdded(IAssetDependency dependencyAdded)
        {

        }

        /// <summary>
        /// Child derived classes can override this method to receive notifications of a
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
            switch (e.Action)
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
            if (m_Dependencies.Count == 0)
            {
                return IsRootAssetDependency();
            }

            foreach (var dependency in m_Dependencies)
            {
                if (dependency.BelongsToRootAssetBranch())
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
            if (IsRootAssetDependency())
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
    }

    public interface ISmartItem
    {
        void AddedToList();
        void RemovedFromList();
    }


    public class SmartItemList<T> : List<T> where T : ISmartItem
    {
        private readonly IList<T> m_List = new List<T>();

        public delegate void OnItemChangeDelegateHandler(T itemThatChanged);
        public event OnItemChangeDelegateHandler OnItemAddedNotification;
        public event OnItemChangeDelegateHandler OnItemRemovedNotification;

        public new IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        protected virtual void OnItemAdded(T item)
        {
            if (OnItemAddedNotification != null)
            {
                OnItemAddedNotification.Invoke(item);
            }
        }

        protected virtual void OnItemRemoved(T item)
        {
            if (OnItemRemovedNotification != null)
            {
                OnItemRemovedNotification.Invoke(item);
            }
        }

        public new void Add(T item)
        {
            m_List.Add(item);
            item.AddedToList();
        }

        public new void Clear()
        {
            foreach (var item in m_List)
            {
                item.RemovedFromList();
            }
            m_List.Clear();
        }

        public new bool Contains(T item)
        {
            return m_List.Contains(item);
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        public new bool Remove(T item)
        {
            if (m_List.Remove(item))
            {
                item.RemovedFromList();
                return true;
            }
            return false;
        }

        public new int Count
        {
            get { return m_List.Count; }
        }

        public bool IsReadOnly
        {
            get { return m_List.IsReadOnly; }
        }

        public new int IndexOf(T item)
        {
            return m_List.IndexOf(item);
        }

        public new void Insert(int index, T item)
        {
            if (m_List.Count > index)
            {
                m_List.Insert(index, item);
                item.AddedToList();
            }
        }

        public new void RemoveAt(int index)
        {
            if (m_List.Count > index)
            {
                T item = m_List[index];
                m_List.RemoveAt(index);
                item.RemovedFromList();
            }
        }

        public new T this[int index]
        {
            get { return m_List[index]; }
            set { m_List[index] = value; }
        }
    }
}
