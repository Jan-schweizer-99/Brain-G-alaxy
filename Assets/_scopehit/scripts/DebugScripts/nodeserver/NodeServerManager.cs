using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

#if UNITY_EDITOR
[CustomEditor(typeof(NodeServerManager))]
public class NodeServerManagerEditor : CustomBaseEditor
{
    protected override void OnEnable()
    {
        SetEditorStyle("Network");
    }

}
#endif
public class NodeServerManager : MonoBehaviour
{
    private Process nodeServer;
    
    [SerializeField]
    private string serverPath = "NodeServer/server.js";  // Pfad zur server.js im Assets-Ordner
    
    [SerializeField]
    private WebSocketClient webSocketClient;  // Referenz auf den WebSocketClient
    
    [SerializeField]
    private int port = 8080;  // Dein WebSocket Port
    
    void Start()
    {
        StartNodeServer();
        SetupWebSocket();
    }
    
    void SetupWebSocket()
    {
        if(webSocketClient != null)
        {
            string ip = GetLocalIPAddress();
            string wsUrl = $"ws://{ip}:{port}";
            webSocketClient.serverUrl = wsUrl;
            UnityEngine.Debug.Log($"WebSocket URL automatisch gesetzt auf: {wsUrl}");
        }
        else
        {
            UnityEngine.Debug.LogError("WebSocketClient Referenz fehlt! Bitte im Inspector zuweisen.");
        }
    }
    
    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "localhost";
    }

    void StartNodeServer()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "node";
            startInfo.Arguments = Path.Combine(Application.dataPath, serverPath);
            startInfo.WorkingDirectory = Path.GetDirectoryName(Path.Combine(Application.dataPath, serverPath));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            nodeServer = new Process();
            nodeServer.StartInfo = startInfo;
            nodeServer.EnableRaisingEvents = true;
            
            // Log der Server-Ausgabe
            nodeServer.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.Log($"<color=#00FF00>[Node.js Server]</color> {e.Data}");
            };
            
            // Log der Server-Fehler
            nodeServer.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.LogError($"<color=#FF0000>[Node.js Server Error]</color> {e.Data}");
            };
            
            nodeServer.Start();
            nodeServer.BeginOutputReadLine();
            nodeServer.BeginErrorReadLine();
            
            UnityEngine.Debug.Log($"<color=#00FF00>[Node.js Server]</color> Server wurde gestartet!");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"<color=#FF0000>[Node.js Server Error]</color> Fehler beim Starten des Servers: {e.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (nodeServer != null && !nodeServer.HasExited)
        {
            try
            {
                nodeServer.Kill();
                nodeServer.Dispose();
                UnityEngine.Debug.Log("<color=#00FF00>[Node.js Server]</color> Server wurde beendet!");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"<color=#FF0000>[Node.js Server Error]</color> Fehler beim Beenden des Servers: {e.Message}");
            }
        }
    }

    // Optional: Methode zum manuellen Neustarten des Servers
    public void RestartServer()
    {
        if (nodeServer != null && !nodeServer.HasExited)
        {
            nodeServer.Kill();
            nodeServer.Dispose();
        }
        StartNodeServer();
        SetupWebSocket();
    }
}