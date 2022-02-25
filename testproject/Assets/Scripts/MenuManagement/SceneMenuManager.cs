using System.Collections.Generic;
/// <summary>
/// The scene menu manager that accepts only SceneReference types
/// </summary>
public class SceneMenuManager : MenuManager<SceneReference>
{
    public static List<SceneMenuManager> SceneMenuManagers = new List<SceneMenuManager>();

    public List<List<string>> GetAllSceneReferences()
    {
        var allSceneReferences = new List<List<string>>();
        foreach (var keypair in m_SceneMenuReferencesByDisplayName)
        {
            allSceneReferences.Add(new List<string>(keypair.Value.GetReferencedScenes()));
        }
        return allSceneReferences;
    }


    protected override void OnAwake()
    {
        SceneMenuManagers.Add(this);
    }

    protected override void OnDestroyInvoked()
    {
        SceneMenuManagers.Remove(this);
    }
}
