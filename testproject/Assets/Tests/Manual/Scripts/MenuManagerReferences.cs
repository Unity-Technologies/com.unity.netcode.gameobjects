using System.Collections.Generic;

namespace TestProject.ManualTests
{
    public static class MenuManagerReferences
    {
        public static List<MainMenuManager> GetMenuMaangers()
        {
            return MainMenuManager.Managers;
        }

        public static List<SceneMenuManager> GetSceneMenuManagers()
        {
            return SceneMenuManager.SceneMenuManagers;
        }
    }
}
