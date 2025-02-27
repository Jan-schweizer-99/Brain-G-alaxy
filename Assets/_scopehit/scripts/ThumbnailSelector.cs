using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ThumbnailSelector : MonoBehaviour
{
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private URLImageLoader thumbnailLoader;
    
    [SerializeField] private int selectedVideoIndex = 0;
    
    private ChannelData channelData;
    
    void Start()
    {
        if (Application.isPlaying)
        {
            LoadChannelData();
            UpdateThumbnail();
        }
    }
    
    public void LoadChannelData()
    {
        if (jsonFile != null)
        {
            try
            {
                channelData = JsonUtility.FromJson<ChannelData>(jsonFile.text);
                Debug.Log($"Loaded {channelData.videos.Length} videos from channel {channelData.channel_name}");
            }
            catch (Exception e)
            {
                Debug.LogError("Error parsing JSON: " + e.Message);
            }
        }
    }
    
    public void UpdateThumbnail()
    {
        if (channelData == null || channelData.videos == null || 
            channelData.videos.Length == 0 || thumbnailLoader == null)
            return;
        
        // Ensure index is within bounds
        selectedVideoIndex = Mathf.Clamp(selectedVideoIndex, 0, channelData.videos.Length - 1);
        
        // Get the new thumbnail URL
        string newUrl = channelData.videos[selectedVideoIndex].thumbnail_url;
        
        // Update the thumbnail loader's URL
        if (thumbnailLoader.imageUrl != newUrl)
        {
            thumbnailLoader.imageUrl = newUrl;
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Force the URLImageLoader's OnValidate to run by marking it dirty
                EditorUtility.SetDirty(thumbnailLoader);
                
                // Call EditorLoadImage method directly via reflection to ensure it updates
                var method = typeof(URLImageLoader).GetMethod("EditorLoadImage", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(thumbnailLoader, null);
                }
            }
            #endif
        }
    }
    
    public void SelectNextVideo()
    {
        if (channelData == null || channelData.videos == null || channelData.videos.Length == 0)
            return;
        
        selectedVideoIndex = (selectedVideoIndex + 1) % channelData.videos.Length;
        UpdateThumbnail();
    }
    
    public void SelectPreviousVideo()
    {
        if (channelData == null || channelData.videos == null || channelData.videos.Length == 0)
            return;
        
        selectedVideoIndex = (selectedVideoIndex - 1 + channelData.videos.Length) % channelData.videos.Length;
        UpdateThumbnail();
    }
    
    public string GetCurrentVideoTitle()
    {
        if (channelData != null && channelData.videos != null && 
            selectedVideoIndex >= 0 && selectedVideoIndex < channelData.videos.Length)
        {
            return channelData.videos[selectedVideoIndex].title;
        }
        return "No video selected";
    }
    
    #if UNITY_EDITOR
    // Add this to ensure the component initializes properly
    [InitializeOnLoadMethod]
    static void OnProjectLoadedInEditor()
    {
        EditorApplication.delayCall += () => {
            var selectors = FindObjectsOfType<ThumbnailSelector>();
            foreach (var selector in selectors)
            {
                if (selector != null)
                {
                    selector.LoadChannelData();
                    selector.UpdateThumbnail();
                }
            }
        };
    }
    
    private void OnValidate()
    {
        // Use EditorApplication.delayCall to ensure this runs after serialization
        EditorApplication.delayCall += () => {
            if (this == null) return; // In case the component is destroyed
            
            LoadChannelData();
            UpdateThumbnail();
        };
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(ThumbnailSelector))]
public class ThumbnailSelectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ThumbnailSelector selector = (ThumbnailSelector)target;
        
        // Check for changes to the inspector
        EditorGUI.BeginChangeCheck();
        
        // Draw default inspector
        DrawDefaultInspector();
        
        // If properties changed, make sure we update
        if (EditorGUI.EndChangeCheck())
        {
            selector.LoadChannelData();
            selector.UpdateThumbnail();
        }
        
        // Add space for better readability
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Thumbnail Controls", EditorStyles.boldLabel);
        
        // Display selected video information
        EditorGUILayout.LabelField("Current Video:", selector.GetCurrentVideoTitle());
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Previous Thumbnail"))
        {
            Undo.RecordObject(selector, "Select Previous Video");
            selector.SelectPreviousVideo();
        }
        
        if (GUILayout.Button("Next Thumbnail"))
        {
            Undo.RecordObject(selector, "Select Next Video");
            selector.SelectNextVideo();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
#endif