#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressablesBuilder
{
    static public bool PreExport()
    {
        if (AddressableAssetSettingsDefaultObject.Settings != null)
        {
            Debug.Log("AddressablesBuilder.PreExport start");
            AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("AddressablesBuilder.PreExport done");
            return true;
        }

        return false;
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        AddressableAssetSettings.OnModificationGlobal += ModificationHandler;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (PreExport())
        {
            EditorApplication.update -= Update;
        }
    }

    private static void ModificationHandler(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent eventType, object obj)
    {
        PreExport();
    }
}
#endif
