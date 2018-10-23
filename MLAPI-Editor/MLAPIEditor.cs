using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class VersionUpgradePopup : EditorWindow
{
    private Vector2 scrollPos = Vector2.zero;
    private GithubRelease[] releases = new GithubRelease[0];
    private Action<bool> continuationAction = null;
    public void SetData(GithubRelease[] releases, Action<bool> continuationAction)
    {
        this.releases = releases;
        this.continuationAction = continuationAction;
    }

    public void OnGUI()
    {
        float padding = 20f;
        float extraPaddingBottom = 30f;
        GUILayout.BeginArea(new Rect(padding, padding, position.width - (padding * 2f), (position.height - (padding * 2f)) - extraPaddingBottom));
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUIStyle warningStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
        warningStyle.normal.textColor = Color.yellow;
        warningStyle.alignment = TextAnchor.MiddleCenter;
        warningStyle.fontStyle = FontStyle.Bold;

        GUIStyle errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
        errorStyle.normal.textColor = Color.red;
        errorStyle.alignment = TextAnchor.MiddleCenter;
        errorStyle.fontStyle = FontStyle.Bold;


        EditorGUILayout.LabelField("The version you are upgrading to has a greater major version. This means that there are non backwards compatibile changes. \n" +
            "For more information about the MLAPI versioning, please visit SemVer.org", warningStyle);
        EditorGUILayout.LabelField("It's ALWAYS recommended to do a backup when upgrading major versions. If your project doesn't compile " +
            "There is good chance serialized data will be PERMANENTLY LOST. Don't be stupid.", errorStyle);
        EditorGUILayout.LabelField("Here are the versions with breaking changes you are skipping.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(5);
        for (int i = 0; i < releases.Length; i++)
        {
            string bodySummary = releases[i].body.Substring(0, releases[i].body.Length > 100 ? 100 : releases[i].body.Length);
            if (releases[i].body.Length > 100) bodySummary += "...";
            EditorGUILayout.LabelField(releases[i].tag_name + ": " + releases[i].name + (bodySummary.Trim().Length > 0 ? (" - " + bodySummary) : ""));
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        float buttonHeight = 30f;
        float buttonWidth = 120f;
        GUILayout.BeginArea(new Rect((position.width / 2f) - buttonWidth, position.height - buttonHeight, buttonWidth * 2f, buttonHeight));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel"))
        {
            if (continuationAction != null)
                continuationAction(false);
            this.Close();
        }
        if (GUILayout.Button("I Understand"))
        {
            if (continuationAction != null)
                continuationAction(true);
            this.Close();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    public void OnLostFocus()
    {
        this.Focus();
    }
}

//SemVer.org
[Serializable]
public struct MLAPIVersion
{
    public byte MAJOR;
    public byte MINOR;
    public byte PATCH;

    public static bool operator >(MLAPIVersion v, MLAPIVersion v1)
    {
        return v.MAJOR > v1.MAJOR || (v.MAJOR == v1.MAJOR && (v.MINOR > v1.MINOR || (v.MINOR == v1.MINOR && (v.PATCH > v1.PATCH))));
    }

    public static bool operator <(MLAPIVersion v, MLAPIVersion v1)
    {
        return v.MAJOR < v1.MAJOR || (v.MAJOR == v1.MAJOR && (v.MINOR < v1.MINOR || (v.MINOR == v1.MINOR && (v.PATCH < v1.PATCH))));
    }

    public static bool operator >=(MLAPIVersion v1, MLAPIVersion v2)
    {
        return v1 == v2 || v1 > v2;
    }

    public static bool operator <=(MLAPIVersion v1, MLAPIVersion v2)
    {
        return v1 == v2 || v1 < v2;
    }

    public static bool operator ==(MLAPIVersion v1, MLAPIVersion v2)
    {
        return v1.MAJOR == v2.MAJOR && v1.MINOR == v2.MINOR && v1.PATCH == v2.PATCH;
    }

    public static bool operator !=(MLAPIVersion v1, MLAPIVersion v2)
    {
        return !(v1 == v2);
    }

    public static MLAPIVersion GetVersionDiff(MLAPIVersion v1, MLAPIVersion v2)
    {
        return new MLAPIVersion()
        {
            MAJOR = (byte)(v1.MAJOR - v2.MAJOR),
            MINOR = (byte)(v1.MINOR - v2.MINOR),
            PATCH = (byte)(v1.PATCH - v2.PATCH)
        };
    }

    public static MLAPIVersion Parse(string version)
    {
        if (version == "None") return new MLAPIVersion()
        {
            MAJOR = byte.MaxValue,
            MINOR = byte.MaxValue,
            PATCH = byte.MaxValue
        };

        string v = version;
        if (version[0] == 'v')
        {
            v = version.Substring(1);
        }
        string[] parts = v.Split('.');
        return new MLAPIVersion()
        {
            MAJOR = byte.Parse(parts[0]),
            MINOR = byte.Parse(parts[1]),
            PATCH = byte.Parse(parts[2])
        };
    }

    public bool IsValid()
    {
        return !(MAJOR == byte.MaxValue && MINOR == byte.MaxValue && PATCH == byte.MaxValue);
    }

    public override string ToString()
    {
        return MAJOR + "." + MINOR + "." + PATCH;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MLAPIVersion))
            return false;

        var version = (MLAPIVersion)obj;
        return MAJOR == version.MAJOR &&
               MINOR == version.MINOR &&
               PATCH == version.PATCH;
    }

    public override int GetHashCode()
    {
        var hashCode = 631069609;
        hashCode = hashCode * -1521134295 + MAJOR.GetHashCode();
        hashCode = hashCode * -1521134295 + MINOR.GetHashCode();
        hashCode = hashCode * -1521134295 + PATCH.GetHashCode();
        return hashCode;
    }
}

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
    private bool canRefetch
    {
        get
        {
            return !(isFetching || isParsing);
        }
    }

    private string statusMessage;

    private int tab;

    private bool showProgressBar = false;
    private float progressTarget = 0f;
    private float progress = 0f;

    [SerializeField]
    private bool PendingPackageLock = false;
    [SerializeField]
    private List<string> PendingPackages = new List<string>();


    [MenuItem("Window/MLAPI")]
    public static void ShowWindow()
    {
        GetWindow<MLAPIEditor>();
    }

    Vector2 scrollPos = Vector2.zero;
    private void OnGUI()
    {
        if (showProgressBar)
        {
            EditorUtility.DisplayProgressBar("Installing...", statusMessage, progress / progressTarget);
        }
        else
        {
            EditorUtility.ClearProgressBar();
        }

        if (PendingPackages.Count > 0 && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !PendingPackageLock)
        {
            PendingPackageLock = true;

            string packageName = PendingPackages[PendingPackages.Count - 1];
            PendingPackages.RemoveAt(PendingPackages.Count - 1);

            AssetDatabase.importPackageCompleted += OnPackageImported;
            AssetDatabase.importPackageFailed += OnPackageImportFailed;

            AssetDatabase.ImportPackage(Application.dataPath + "/MLAPI/Lib/" + packageName, false);
        }

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
            EditorGUILayout.LabelField("Not yet implemented. The REST API for AppVeyor is proper garbage and is needed to grab the artifact download URLs", EditorStyles.wordWrappedMiniLabel);
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

    private void OnPackageImported(string packageName)
    {
        AssetDatabase.importPackageCompleted -= OnPackageImported;
        PendingPackageLock = false;
    }

    private void OnPackageImportFailed(string packageName, string errorMessage)
    {
        AssetDatabase.importPackageFailed -= OnPackageImportFailed;
        PendingPackageLock = false;
    }

    private List<MLAPIVersion> GetMajorVersionsBetween(MLAPIVersion currentVersion, MLAPIVersion targetVersion)
    {
        List<MLAPIVersion> versionsBetween = new List<MLAPIVersion>();
        for (int i = 0; i < releases.Length; i++)
        {
            MLAPIVersion version = MLAPIVersion.Parse(releases[i].tag_name);
            if (version >= currentVersion && version <= targetVersion)
            {
                MLAPIVersion diff = MLAPIVersion.GetVersionDiff(currentVersion, version);
                if (diff.MAJOR > 0)
                {
                    versionsBetween.Add(version);
                }
            }
        }
        return versionsBetween;
    }

    private GithubRelease GetReleaseOfVersion(MLAPIVersion version)
    {
        for (int i = 0; i < releases.Length; i++)
        {
            if (MLAPIVersion.Parse(releases[i].tag_name) == version)
                return releases[i];
        }
        return null;
    }

    private GithubRelease[] GetReleasesFromVersions(List<MLAPIVersion> versions)
    {
        GithubRelease[] releases = new GithubRelease[versions.Count];
        for (int i = 0; i < versions.Count; i++)
        {
            releases[i] = GetReleaseOfVersion(versions[i]);
        }
        return releases;
    }

    private IEnumerator InstallRelease(int index)
    {
        PendingPackages.Clear();
        PendingPackageLock = true;
        bool waiting = true;
        bool accepted = false;
        MLAPIVersion currentMLAPIVersion = MLAPIVersion.Parse(currentVersion);
        List<MLAPIVersion> versions = GetMajorVersionsBetween(currentMLAPIVersion, MLAPIVersion.Parse(releases[index].tag_name));
        if (versions.Count > 0 && currentMLAPIVersion.IsValid() && currentMLAPIVersion > new MLAPIVersion() { MAJOR = 2, MINOR = 0, PATCH = 0 })
        {
            VersionUpgradePopup window = ScriptableObject.CreateInstance<VersionUpgradePopup>();
            Rect mainWindowRect = MLAPIEditorExtensions.GetEditorMainWindowPos();
            float widthFill = 0.5f;
            float heightFill = 0.3f;
            window.position = new Rect(mainWindowRect.center.x - ((mainWindowRect.width * widthFill) / 2f), mainWindowRect.center.y - ((mainWindowRect.height * heightFill) / 2f), mainWindowRect.width * widthFill, mainWindowRect.height * heightFill);
            window.SetData(GetReleasesFromVersions(versions), (result) =>
            {
                //Called if the user proceeds with the upgrade.
                accepted = result;
                waiting = false;
            });
            window.ShowPopup();
        }
        else
        {
            accepted = true;
            waiting = false;
        }

        while (waiting) yield return null;

        if (accepted)
        {
            showProgressBar = true;
            progressTarget = releases[index].assets.Length;
            progress = 0;

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
                        PendingPackages.Add(releases[index].assets[i].name);
                    }
                    yield return null;
                }
                progress = i;
            }

            yield return null;
            statusMessage = "";
            if (!downloadFail)
                currentVersion = releases[index].tag_name; //Only set this if there was no fail. This is to allow them to still retry the download
            AssetDatabase.Refresh();
        }
        showProgressBar = false;
        statusMessage = "";
        PendingPackageLock = false;
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

//https://answers.unity.com/questions/960413/editor-window-how-to-center-a-window.html
public static class MLAPIEditorExtensions
{
    public static Type[] GetAllDerivedTypes(this AppDomain appDomain, Type type)
    {
        List<Type> result = new List<Type>();
        Assembly[] assemblies = appDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type[] types = assembly.GetTypes();
            foreach (Type myType in types)
            {
                if (myType.IsSubclassOf(type))
                    result.Add(myType);
            }
        }
        return result.ToArray();
    }

    public static Rect GetEditorMainWindowPos()
    {
        Type containerWinType = AppDomain.CurrentDomain.GetAllDerivedTypes(typeof(ScriptableObject)).Where(t => t.Name == "ContainerWindow").FirstOrDefault();
        if (containerWinType == null)
            throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
        FieldInfo showModeField = containerWinType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
        PropertyInfo positionProperty = containerWinType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
        if (showModeField == null || positionProperty == null)
            throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
        UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(containerWinType);
        foreach (UnityEngine.Object win in windows)
        {
            int showmode = (int)showModeField.GetValue(win);
            if (showmode == 4) // main window
            {
                Rect pos = (Rect)positionProperty.GetValue(win, null);
                return pos;
            }
        }
        throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
    }
}