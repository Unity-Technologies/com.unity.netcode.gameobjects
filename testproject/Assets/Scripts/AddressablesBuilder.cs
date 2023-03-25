#if TESTPROJECT_USE_ADDRESSABLES
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AddressablesBuilder
{
    public static void PreExport()
    {
        if (AddressableAssetSettingsDefaultObject.Settings != null)
        {
            Debug.Log("AddressablesBuilder.PreExport start");
            AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("AddressablesBuilder.PreExport done");
        }
    }
}
#endif
#endif
