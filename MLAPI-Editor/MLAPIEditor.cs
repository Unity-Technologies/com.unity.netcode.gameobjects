using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[Serializable]
public class GithubRelease
{
    public string html_url;
    public string tag_name;
    public string name;
    public string body;
    public string published_at;
    public bool prerelease;
    public GithubAsset[] assets;
}

[Serializable]
public class GithubAsset
{
    public string browser_download_url;
    public string name;
}

[Serializable]
public class AppveyorProject
{
    public int repositoryBranch;
}

[Serializable]
public class AppveyorBuild
{

}

[InitializeOnLoad]
public class MLAPIEditor : EditorWindow
{
    private const string API_URL = "https://api.github.com/repos/MidLevel/MLAPI/releases";
    private GithubRelease[] releases = new GithubRelease[0];
    private bool[] foldoutStatus = new bool[0];
    private string currentVersion
    {
        get
        {
            return EditorPrefs.GetString("MLAPI_version", "None");
        }
        set
        {
            EditorPrefs.SetString("MLAPI_version", value);
        }
    }
    private long lastUpdated
    {
        get
        {
            return Convert.ToInt64(EditorPrefs.GetString("MLAPI_lastUpdated", "0"));
        }
        set
        {
            EditorPrefs.SetString("MLAPI_lastUpdated", Convert.ToString(value));
        }
    }

    private bool isFetching = false;
    private bool isParsing = false;
    private bool canRefetch => !(isFetching || isParsing);
    private string statusMessage;

    private int tab;

    [MenuItem("Window/MLAPI")]
    public static void ShowWindow()
    {
        GetWindow<MLAPIEditor>();
    }

    Vector2 scrollPos = Vector2.zero;
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(5, 0, position.width - 5, position.height - (40 + ((string.IsNullOrEmpty(statusMessage) ? 0 : 20) + (canRefetch ? 20 : 0)))));
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        tab = GUILayout.Toolbar(tab, new string[] { "GitHub", "Commits" });
        if (tab == 0)
        {
            if (foldoutStatus != null)
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

                        if (currentVersion != releases[i].tag_name && GUILayout.Button("Install"))
                            EditorCoroutine.Start(InstallRelease(i));

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
        else if (tab == 1)
        {
            EditorGUILayout.LabelField("Not yet implemented. The rest API for AppVeyor is proper garbage and is needed to grab the artifact download URLs", EditorStyles.wordWrappedMiniLabel);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(5, position.height - (40 + ((string.IsNullOrEmpty(statusMessage) ? 0 : 20) + (canRefetch ? 20 : 0))), position.width - 5, (60 + ((string.IsNullOrEmpty(statusMessage) ? 0 : 20) + (canRefetch ? 20 : 0)))));

        string lastUpdatedString = lastUpdated == 0 ? "Never" : new DateTime(lastUpdated).ToShortTimeString();
        GUILayout.Label("Last checked: " + lastUpdatedString, EditorStyles.centeredGreyMiniLabel);

        if (canRefetch && GUILayout.Button("Fetch releases"))
            EditorCoroutine.Start(GetReleases());
        if (!string.IsNullOrEmpty(statusMessage))
            GUILayout.Label(statusMessage, EditorStyles.centeredGreyMiniLabel);
        if (GUILayout.Button("Reset defaults"))
        {
            releases = new GithubRelease[0];
            foldoutStatus = new bool[0];
            if (EditorPrefs.HasKey("MLAPI_version")) EditorPrefs.DeleteKey("MLAPI_version");
            if (EditorPrefs.HasKey("MLAPI_lastUpdated")) EditorPrefs.DeleteKey("MLAPI_lastUpdated");
        }

        GUILayout.EndArea();

        if ((releases.Length == 0 && (DateTime.Now - new DateTime(lastUpdated)).TotalSeconds > 600) || (DateTime.Now - new DateTime(lastUpdated)).TotalSeconds > 3600)
            EditorCoroutine.Start(GetReleases());

        Repaint();
    }

    private IEnumerator InstallRelease(int index)
    {
        statusMessage = "Cleaning lib folder";
        yield return null;

        if (Directory.Exists(Application.dataPath + "/MLAPI/Lib/"))
            Directory.Delete(Application.dataPath + "/MLAPI/Lib/", true);

        Directory.CreateDirectory(Application.dataPath + "/MLAPI/Lib/");

        bool downloadFail = false;
        for (int i = 0; i < releases[index].assets.Length; i++)
        {
            WWW www = new WWW(releases[index].assets[i].browser_download_url);
            while (!www.isDone && string.IsNullOrEmpty(www.error))
            {
                statusMessage = "Downloading " + releases[index].assets[i].name + "(" + (i + 1) + "/" + releases[index].assets.Length + ") " + www.progress + "%";
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                //Some kind of error
                downloadFail = true;
                statusMessage = "Failed to download asset " + releases[index].assets[i].name + ". Error: " + www.error;
                double startTime = EditorApplication.timeSinceStartup;
                //Basically = yield return new WaitForSeconds(5);
                while (EditorApplication.timeSinceStartup - startTime <= 5f)
                    yield return null;
                statusMessage = "";
            }
            else
            {
                statusMessage = "Writing " + releases[index].assets[i].name + " to disk";
                yield return null;

                File.WriteAllBytes(Application.dataPath + "/MLAPI/Lib/" + releases[index].assets[i].name, www.bytes);

                if (releases[index].assets[i].name.EndsWith(".unitypackage"))
                {
                    statusMessage = "Installing " + releases[index].assets[i].name;
                    yield return null;
                    AssetDatabase.ImportPackage(Application.dataPath + "/MLAPI/Lib/" + releases[index].assets[i].name, false);
                }

                yield return null;
            }
        }

        yield return null;
        statusMessage = "";
        if (!downloadFail)
            currentVersion = releases[index].tag_name; //Only set this if there was no fail. This is to allow them to still retry the download
        AssetDatabase.Refresh();
    }


    private IEnumerator GetReleases()
    {
        lastUpdated = DateTime.Now.Ticks;

        WWW www = new WWW(API_URL);
        isFetching = true;
        while (!www.isDone && string.IsNullOrEmpty(www.error))
        {
            statusMessage = "Fetching releases " + www.progress + "%";
            yield return null;
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            //Some kind of error
            statusMessage = "Failed to fetch releases. Error: " + www.error;
            double startTime = EditorApplication.timeSinceStartup;
            //Basically = yield return new WaitForSeconds(5);
            while (EditorApplication.timeSinceStartup - startTime <= 5f)
                yield return null;
            statusMessage = "";
        }
        else
        {
            isFetching = false;
            isParsing = true;
            string json = www.text;

            //This makes it from a json array to the individual objects in the array. 
            //The JSON serializer cant take arrays. We have to split it up outselves.
            List<string> releasesJson = new List<string>();
            int depth = 0;
            StringBuilder builder = new StringBuilder();
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
                    builder.Append(json[i]);

                if (depth == 0 && json[i] == ',')
                {
                    releasesJson.Add(builder.ToString());
                    builder.Length = 0;
                }

                //Parse in smaller batches
                if (i % (json.Length / 100) == 0)
                {
                    statusMessage = "Splitting JSON " + (i / (float)json.Length) * 100f + "%";
                    yield return null;
                }

                statusMessage = "";
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

                if (i % (releasesJson.Count / 30f) == 0)
                {
                    yield return null;
                    statusMessage = "Parsing JSON " + (i / (float)releasesJson.Count) * 100f + "%";
                }
            }

            statusMessage = "";
            isParsing = false;
        }
    }

    public class EditorCoroutine
    {
        public static EditorCoroutine Start(IEnumerator routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }

        private readonly IEnumerator coroutine;
        EditorCoroutine(IEnumerator routine)
        {
            coroutine = routine;
        }

        private void Start()
        {
            EditorApplication.update += Update;
        }

        private void Stop()
        {
            EditorApplication.update -= Update;
        }

        void Update()
        {
            if (!coroutine.MoveNext()) Stop();
        }
    }
}
