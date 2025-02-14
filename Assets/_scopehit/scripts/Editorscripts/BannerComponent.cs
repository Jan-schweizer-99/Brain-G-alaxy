#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Net;
using System.Linq;

public class BannerClass : MonoBehaviour 
{
    [HideInInspector]
    public bool hideFlags = true;
}

[CustomEditor(typeof(BannerClass))]
public class BannerClassEditor : Editor 
{
    protected Color backgroundColor = Color.black;
    protected Texture2D bannerTexture;
    protected const float BANNER_HEIGHT = 80f;
    protected const float PADDING = 10f;
    protected float adjustedBannerHeight;
    protected GUIStyle textStyle;
    protected string ipAddress;

    protected virtual string BannerPath => "Assets/Editor/Banners/banner.png";
    protected virtual float AdditionalBackgroundHeight => adjustedBannerHeight + PADDING;

    protected virtual void OnEnable()
    {
        LoadBanner();
        backgroundColor = backgroundColor;
        SetupTextStyle();
        GetLocalIPAddress();
    }

    private void SetupTextStyle()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.fontSize = 14;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = Color.green;
        }
    }

    private void GetLocalIPAddress()
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(Dns.GetHostName());
            ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString() ?? "IP not found";
        }
        catch
        {
            ipAddress = "IP not found";
        }
    }

    private void LoadBanner()
    {
        if (!string.IsNullOrEmpty(BannerPath))
        {
            bannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BannerPath);
            Debug.Log($"Attempting to load banner from: {BannerPath}");
            Debug.Log($"Banner loaded successfully: {bannerTexture != null}");

            if (bannerTexture == null)
            {
                Debug.LogWarning($"Banner not found at {BannerPath}");
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null) return;

        serializedObject.Update();

        DrawCustomBackground();
        DrawBanner();
        DrawNetworkText();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void DrawCustomBackground()
    {
        float startY = EditorGUIUtility.singleLineHeight;
        float height = adjustedBannerHeight + PADDING * 2;
        Rect backgroundRect = new Rect(
            0,
            startY - 20,
            EditorGUIUtility.currentViewWidth,
            height
        );
        EditorGUI.DrawRect(backgroundRect, backgroundColor);
    }

    protected virtual void DrawBanner()
    {
        float startY = EditorGUIUtility.singleLineHeight;
        float bannerWidth = EditorGUIUtility.currentViewWidth - (PADDING * 2);
        float aspectRatio = (float)bannerTexture.width / bannerTexture.height;
        adjustedBannerHeight = bannerWidth / aspectRatio;

        Rect bannerRect = new Rect(
            PADDING,
            startY,
            bannerWidth,
            adjustedBannerHeight
        );

        if (bannerTexture != null)
        {
            GUI.DrawTexture(bannerRect, bannerTexture, ScaleMode.ScaleToFit);
        }

        GUILayout.Space(adjustedBannerHeight + PADDING);
    }

    protected virtual void DrawNetworkText()
    {
        float buttonWidth = 60f;
        float spacing = 5f;
        
        Rect textRect = new Rect(
            PADDING,
            EditorGUIUtility.singleLineHeight,
            EditorGUIUtility.currentViewWidth - (PADDING * 2) - buttonWidth - spacing,
            30
        );
        
        string url = $"http://{ipAddress}:3000";
        EditorGUI.LabelField(textRect, $"Netzwerk Debugcontroller: {url}", textStyle);

        Rect buttonRect = new Rect(
            textRect.xMax + spacing,
            textRect.y,
            buttonWidth,
            20
        );

        if (GUI.Button(buttonRect, "Copy"))
        {
            GUIUtility.systemCopyBuffer = url;
            Debug.Log("URL copied to clipboard!");
        }
    }

    protected float GetInspectorHeight()
    {
        if (serializedObject == null) return 0f;

        float totalHeight = EditorGUIUtility.standardVerticalSpacing;
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            totalHeight += EditorGUI.GetPropertyHeight(iterator, true) +
                          EditorGUIUtility.standardVerticalSpacing;
        }

        return totalHeight;
    }
}
#endif