using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MLAPIEditor : EditorWindow
{
    private GithubRelease[] releases = new GithubRelease[0];
    private bool[] foldoutStatus = new bool[0];
    private long lastUpdated = 0;
    private string currentVersion;

    [MenuItem("Window/MLAPI")]
    public static void ShowWindow()
    {
        GetWindow<MLAPIEditor>();
    }

    private void Init()
    {
        lastUpdated = 0;

        if (EditorPrefs.HasKey("MLAPI_version"))
            currentVersion = EditorPrefs.GetString("MLAPI_version");
        else
            currentVersion = "None";
    }

    private void Awake()
    {
        Init();
    }

    private void OnFocus()
    {
        Init();
    }

    private void OnGUI()
    {
        if(foldoutStatus != null)
        {
            for (int i = 0; i < foldoutStatus.Length; i++)
            {
                if (releases[i] == null)
                    continue;
                foldoutStatus[i] = EditorGUILayout.Foldout(foldoutStatus[i], releases[i].tag_name + " - " + releases[i].name);
                if (foldoutStatus[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Release notes", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(releases[i].body, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    if (releases[i].prerelease)
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                        style.normal.textColor = new Color(1f, 0.5f, 0f);
                        EditorGUILayout.LabelField("Pre-release", style);
                    }
                    else
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                        style.normal.textColor = new Color(0f, 1f, 0f);
                        EditorGUILayout.LabelField("Stable-release", style);
                    }
                    if (currentVersion == releases[i].tag_name)
                    {
                        GUIStyle boldStyle = new GUIStyle(EditorStyles.boldLabel);
                        boldStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
                        EditorGUILayout.LabelField("Installed", boldStyle);
                    }
                    EditorGUILayout.LabelField("Release date: " + DateTime.Parse(DateTime.Parse(releases[i].published_at).ToString()), EditorStyles.miniBoldLabel);

                    if(currentVersion != releases[i].tag_name && GUILayout.Button("Install"))
                        InstallRelease(i);

                    EditorGUI.indentLevel--;
                }
            }
        }

        GUILayout.BeginArea(new Rect(5, position.height - 20, position.width, 20));

        if (GUILayout.Button("Check for updates"))
            GetReleases();

        GUILayout.EndArea();

        string lastUpdatedString = lastUpdated == 0 ? "Never" : new DateTime(lastUpdated).ToShortTimeString();
        EditorGUI.LabelField(new Rect(5, position.height - 40, position.width, 20), "Last checked: " + lastUpdatedString, EditorStyles.centeredGreyMiniLabel);

        if ((DateTime.Now - new DateTime(lastUpdated)).Seconds > 3600)
            GetReleases();

        Repaint();
    }

    private void InstallRelease(int index)
    {
        for (int i = 0; i < releases[index].assets.Length; i++)
        {
            WWW www = new WWW(releases[index].assets[i].browser_download_url);
            while (!www.isDone && string.IsNullOrEmpty(www.error))
            {
                EditorGUI.ProgressBar(new Rect(5, position.height - 60, position.width, 20), www.progress, "Installing " + i + "/" + releases[index].assets.Length);
            }

            if(!Directory.Exists(Application.dataPath + "/MLAPI/Lib/"))
                Directory.CreateDirectory(Application.dataPath + "/MLAPI/Lib/");

            File.WriteAllBytes(Application.dataPath + "/MLAPI/Lib/" + releases[index].assets[i].name, www.bytes);

            if (releases[index].assets[i].name.EndsWith(".unitypackage"))
                AssetDatabase.ImportPackage(Application.dataPath + "/MLAPI/Lib/" + releases[index].assets[i].name, true);
        }

        EditorPrefs.SetString("MLAPI_version", releases[index].tag_name);
        currentVersion = releases[index].tag_name;
        AssetDatabase.Refresh();
    }

    private void GetReleases()
    {
        lastUpdated = DateTime.Now.Ticks;

        WWW www = new WWW("https://api.github.com/repos/TwoTenPvP/MLAPI/releases");
        while(!www.isDone && string.IsNullOrEmpty(www.error))
        {
            EditorGUI.ProgressBar(new Rect(5, position.height - 60, position.width, 20), www.progress, "Fetching...");
        }
        string json = www.text;

        //This makes it from a json array to the individual objects in the array. 
        //The JSON serializer cant take arrays. We have to split it up outselves.
        List<string> releasesJson = new List<string>();
        int depth = 0;
        string currentObject = "";
        for (int i = 1; i < json.Length - 1; i++)
        {
            if (json[i] == '[')
                depth++;
            else if (json[i] == ']')
                depth--;
            else if (json[i] == '{')
                depth++;
            else if (json[i] == '}')
                depth--;

            if ((depth == 0 && json[i] != ',') || depth > 0)
                currentObject += json[i];

            if (depth == 0 && json[i] == ',')
            {
                releasesJson.Add(currentObject);
                currentObject = "";
            }
        }

        releases = new GithubRelease[releasesJson.Count];
        foldoutStatus = new bool[releasesJson.Count];

        for (int i = 0; i < releasesJson.Count; i++)
        {
            releases[i] = JsonUtility.FromJson<GithubRelease>(releasesJson[i]);
            if (i == 0)
                foldoutStatus[i] = true;
            else
                foldoutStatus[i] = false;
        }
    }
}
