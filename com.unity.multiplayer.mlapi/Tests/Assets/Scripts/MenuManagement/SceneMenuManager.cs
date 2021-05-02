
using UnityEngine;
namespace MLAPI.TestAssets.MenuManagement
{
    /// <summary>
    /// The scene menu manager that accepts only SceneReference types
    /// </summary>
    [AddComponentMenu("MLAPI/TestAssets/MenuManagement/SceneMenuManager")]
    public class SceneMenuManager : MenuManager<SceneReference>
    {
    }
}
