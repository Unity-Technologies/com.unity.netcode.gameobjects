using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField]
    private List<SceneMenuReference> m_SceneMenus;

    public Dropdown SceneMenusDropDownList;

    [Tooltip("Horizontal window resolution size")]
    public int HorizontalResolution = 1024;

    [Tooltip("Vertical window resolution size")]
    public int VerticalResolution = 768;


    private Dictionary<string, SceneMenuReference> m_SceneMenuReferencesByDisplayName = new Dictionary<string, SceneMenuReference>();

    private Dropdown.OptionDataList m_OptionsList = new Dropdown.OptionDataList();

    private void Awake()
    {
        Screen.SetResolution(HorizontalResolution, VerticalResolution, false);
    }

    private void Start()
    {       
        foreach (var menuReference in m_SceneMenus)
        {
            if(!m_SceneMenuReferencesByDisplayName.ContainsKey(menuReference.DisplayName))
            {
                m_SceneMenuReferencesByDisplayName.Add(menuReference.DisplayName, menuReference);

                var optionData = new Dropdown.OptionData();
                optionData.text = menuReference.DisplayName;

                m_OptionsList.options.Add(optionData);
            }
        }

        SceneMenusDropDownList.options = m_OptionsList.options;
    }

    public void OnSelectMenuScene()
    {
        string selectedMenuScene = SceneMenusDropDownList.options[SceneMenusDropDownList.value].text;
        if (m_SceneMenuReferencesByDisplayName.ContainsKey(selectedMenuScene))
        {
            SceneManager.LoadScene(m_SceneMenuReferencesByDisplayName[selectedMenuScene].SceneName, LoadSceneMode.Single);
        }
    }
}
