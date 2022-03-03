using System;
using UnityEngine;using System.Linq;
public class RemoteDataLoader1 : MonoBehaviour{    public void Awake()    {                var panelObject = GetComponentInParent<Canvas>();        Debug.Log(panelObject);        var textObject = GetComponent<UnityEngine.UI.Text>();        Debug.Log(textObject.text);    }    // Start is called before the first frame update    public void Start()    {        string configData = RemoteConfigUtils.GetRemoteConfig(Version.v1);        var textObject = GetComponent<UnityEngine.UI.Text>();        textObject.text += "\n" + configData;
    }    // Update is called once per frame    public void Update()    {            }}public class RemoteConfigUtils{    public static string GetRemoteConfig(Version version)    {
        // There are three sources of information
        // 1. Command Line
        // 2. Web config
        // 3. Local file config
        // This needs to be ordered carefully as this also represents the order of priority, when one is found the next item on the list is not regarded

        // -m(?) | Start network in one of 3 modes: client, host, server
        bool isCommandLine = Environment.GetCommandLineArgs().Any(value => value == "-m");        return $"isCommandLine: {isCommandLine}";    }}public enum Version{    v1,    v2}