using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TwitchStreamPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage rawImage;

    void Start()
    {
        // Twitch-Stream URL
        string streamUrl = "https://video-weaver.fra05.hls.ttvnw.net/v1/playlist/CqAGOnKuFGHi1RCkSa9tOy4p2u4xzspRdqbzrSfBm_8yv8iel4xrGUJqtHO1p1biJdkYLxxrdHbj2VmQseZDMlnKCTeRDnh5bk8fpPA_5rzKzFMB2jQSxpx4eK8UxPT0M2HMPwM4FDZ2hWdOO8smnXJfdOGU6m9NMwPP5mWw13uMEBOUMtqPTKk11M2wAflJwCSuib2PclD6G3oiYPobh05wT2AH46HGHC01tBb3LTM_kY68rxkT0N9nn4-ZoGtMk9wnjpwMNKGdXl3kgwrktg9y2J0_FxANvqvWwM55Rn5Tbs0qaZzTquMEkVeEtEGOwuMkV8RX-LyTwGov3Fde1Bx7ewCfDIPEMbOLz7fwb3xlOV8tuHOKxUKrAX-LF8KuqSlwC-fcHUb8OJE8Zj6saYapIDKt7pD25Fy7Qr5UjoNvJxXMZ6dF8AfrngFO-NR38KY37h5YEo4q5YowU69MtpyxybpCh6npQVQFKnyrCyqtBiWfUNC3LTqhXLkfFolRF3XY4hYJL-g-U2J_2IJzVbavFs48SnyYfmX8pwOZAEeKmH7_D1PuvNZUzbr8X8lIttjKBJkGR8exziyzYbbmFReoG_LAmv4e3_3U6bGoV7mBeMob-6UfYbAaXXJATUSOlv_VwKpntoHOX0DPMQEZFcxuTma4QN2YMoSMoCA5RPqhDiOmnixnaALPXft3X-Euzti1srlSls1yZOEq6_uEuqSoPSqGQXjft8Y3dxtGQV01bCSp3GMYoEeam2AZYOxgQbk4dGbcafJ73TkZBgYSondu1l4g04PqXwJtAf8DLnLuNtb9NpU2jraFJ2qmeX6TaTPySqeHRpkCNklYD_inPGfIKw-u8BAKJBq2pEgGhXSrkjPDw_8embU1X0gslrA_qixE2eQA1Ve953uXcoJ_9SRrI4ClX-K0abhVPJuoyFW0Tn5hnlFjGnzg24ZWGLdXUJ3y4iOH4ULmmLdw1Iij-a3l5XqYclXHoHPpvAmPqGEqtBqXanuJH41oCEnDWW-Qlw4NSuJjrfugGNYvBxOovtGDjjWqfL37dX5F5PnafHdsEBcaDDw5v2EneiAFd47xcSABKglldS13ZXN0LTIwtQo.m3u8";
        
        // Set URL to the VideoPlayer
        videoPlayer.url = streamUrl;

        // Set the RawImage texture to the VideoPlayer
        videoPlayer.prepareCompleted += Prepared;
    }

    void Prepared(VideoPlayer vp)
    {
        rawImage.texture = videoPlayer.texture;
        videoPlayer.Play();
    }
}
