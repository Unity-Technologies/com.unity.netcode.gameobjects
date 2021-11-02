using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// Main Menu Manager only accepts types of MenuReference
/// </summary>
public class MainMenuManager : MenuManager<MenuReference>
{
    public static List<MainMenuManager> Managers = new List<MainMenuManager>();

    public List<string> GetAllMenuScenes()
    {
        var allSceneReferences = new List<string>();

        foreach (var keypair in m_SceneMenuReferencesByDisplayName)
        {
            allSceneReferences.AddRange(keypair.Value.GetReferencedScenes());
        }
        return allSceneReferences;
    }

    protected override void OnAwake()
    {
        Managers.Add(this);
    }

    protected override void OnDestroyInvoked()
    {
        Managers.Remove(this);
    }
}


/// <summary>
/// Used for to construct a menu that accepts a specific type of ISceneReference
/// </summary>
/// <typeparam name="T"></typeparam>
public class MenuManager<T> : MonoBehaviour where T : ISceneReference
{
    [SerializeField]
    protected List<T> m_SceneMenus;

    [SerializeField]
    protected Dropdown m_SceneMenusDropDownList;

    [HideInInspector]
    [SerializeField]
    protected Dropdown.OptionDataList m_OptionsList = new Dropdown.OptionDataList();

    [Tooltip("Horizontal window resolution size")]
    public int HorizontalResolution = 1024;

    [Tooltip("Vertical window resolution size")]
    public int VerticalResolution = 768;

    protected Dictionary<string, T> m_SceneMenuReferencesByDisplayName = new Dictionary<string, T>();

    protected virtual void OnAwake()
    {

    }

    private void Awake()
    {
        Screen.SetResolution(HorizontalResolution, VerticalResolution, false);
        OnAwake();
    }

    protected virtual void OnBuildMenuList()
    {
        m_OptionsList.options.Clear();
        foreach (var menuReference in m_SceneMenus)
        {
            if (!m_SceneMenuReferencesByDisplayName.ContainsKey(menuReference.GetReferencedScenes()[0]))
            {
                m_SceneMenuReferencesByDisplayName.Add(menuReference.GetDisplayName(), menuReference);

                var optionData = new Dropdown.OptionData();
                optionData.text = menuReference.GetDisplayName();

                m_OptionsList.options.Add(optionData);
            }
        }
    }

    private void Start()
    {
        OnBuildMenuList();

        m_SceneMenusDropDownList.options = m_OptionsList.options;
    }


    protected virtual void OnSelectMenuScene()
    {
        string selectedMenuScene = m_SceneMenusDropDownList.options[m_SceneMenusDropDownList.value].text;
        if (m_SceneMenuReferencesByDisplayName.ContainsKey(selectedMenuScene))
        {
            SceneManager.LoadScene(m_SceneMenuReferencesByDisplayName[selectedMenuScene].GetReferencedScenes()[0], LoadSceneMode.Single);
        }
    }

    public void SelectMenuScene()
    {
        OnSelectMenuScene();
    }

    protected virtual void OnDestroyInvoked()
    {

    }

    private void OnDestroy()
    {
        OnDestroyInvoked();
    }

}
