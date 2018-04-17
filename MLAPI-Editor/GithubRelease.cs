using System;

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
