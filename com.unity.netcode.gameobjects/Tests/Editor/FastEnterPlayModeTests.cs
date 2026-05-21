using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{

    internal class FastEnterPlayModeTests
    {
        [UnityTest]
        public IEnumerator NetworkManagerSingletonResetsOnPlayModeEnter(
            [Values(EnterPlayModeOptions.None,
                EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload)]
            EnterPlayModeOptions playmodeOption)
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = playmodeOption;

            // First play session — create a NetworkManager to set Singleton
            yield return new EnterPlayMode();

            yield return new ExitPlayMode();

            // Restore default settings
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
    }
}
