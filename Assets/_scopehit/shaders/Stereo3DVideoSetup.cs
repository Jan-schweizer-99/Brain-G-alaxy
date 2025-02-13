using UnityEngine;
using UnityEngine.Video;

public class Stereo3DVideoSetup : MonoBehaviour
{
    public VideoPlayer leftEyePlayer;
    public VideoPlayer rightEyePlayer;
    public string leftVideoURL;
    public string rightVideoURL;
    public Material stereo3DMaterial;

    void Start()
    {
        // Erstellen Sie Render Textures für jedes Auge
        RenderTexture leftEyeTexture = new RenderTexture(1920, 1080, 0);
        RenderTexture rightEyeTexture = new RenderTexture(1920, 1080, 0);

        // Konfigurieren Sie die Video Player
        SetupVideoPlayer(leftEyePlayer, leftVideoURL, leftEyeTexture);
        SetupVideoPlayer(rightEyePlayer, rightVideoURL, rightEyeTexture);

        // Weisen Sie die Texturen dem Material zu
        stereo3DMaterial.SetTexture("_LeftTex", leftEyeTexture);
        stereo3DMaterial.SetTexture("_RightTex", rightEyeTexture);
    }

    void SetupVideoPlayer(VideoPlayer player, string url, RenderTexture targetTexture)
    {
        player.url = url;
        player.targetTexture = targetTexture;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.Play();
    }
}