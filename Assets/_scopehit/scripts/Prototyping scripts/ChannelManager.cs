using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class ChannelData 
{
    public string channel_id;
    public string channel_name;
    public string banner_url;
    public string avatar_url;
    public VideoData[] videos;
}

[Serializable]
public class VideoData 
{
    public string id;
    public string title;
    public string description;
    public string url;
    public string thumbnail_url;
}

public class ChannelManager : MonoBehaviour 
{
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private URLImageLoader avatarLoader;
    [SerializeField] private URLImageLoader bannerLoader;
    
    private ChannelData channelData;

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void OnProjectLoadedInEditor()
    {
        EditorApplication.delayCall += () => {
            var managers = FindObjectsOfType<ChannelManager>();
            foreach (var manager in managers)
            {
                manager.LoadChannelData();
                manager.UpdateImages();
            }
        };
    }

    private void OnValidate()
    {
        EditorApplication.delayCall += () => {
            LoadChannelData();
            UpdateImages();
        };
    }
    #endif

    void Start()
    {
        if (Application.isPlaying)
        {
            LoadChannelData();
            UpdateImages();
        }
    }

    void LoadChannelData()
    {
        if (jsonFile != null)
        {
            channelData = JsonUtility.FromJson<ChannelData>(jsonFile.text);
        }
    }

    void UpdateImages()
    {
        if (channelData == null) return;

        if (avatarLoader != null)
        {
            avatarLoader.imageUrl = channelData.avatar_url;
        }

        if (bannerLoader != null)
        {
            bannerLoader.imageUrl = channelData.banner_url;
        }
    }
}