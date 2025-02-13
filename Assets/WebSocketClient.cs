using System.Collections;
using UnityEngine;
using WebSocketSharp;
using LitJson;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    public TextMeshProUGUI tmpField;

    // Die URL deines Node.js WebSocket-Servers
    private string serverUrl = "ws://localhost:8080";

    void Start()
    {
        tmpField = GetComponentInChildren<TextMeshProUGUI>();

        // Verbinde mit dem WebSocket-Server
        ws = new WebSocket(serverUrl);
        ws.OnOpen += (sender, e) => Debug.Log("WebSocket connection opened");
        ws.OnMessage += OnWebSocketMessage;
        ws.OnClose += (sender, e) => Debug.Log("WebSocket connection closed, reason: " + e.Reason);

        StartCoroutine(ConnectWebSocket());
    }

    IEnumerator ConnectWebSocket()
    {
        ws.Connect();
        while (ws.ReadyState != WebSocketState.Open)
        {
            yield return null;
        }
        Debug.Log("WebSocket connection status: " + ws.ReadyState);
    }

    void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        // Verarbeite die empfangene JSON-Nachricht hier
        string jsonString = e.Data;

        // Parse die JSON-Nachricht
        JsonData jsonData = JsonMapper.ToObject(jsonString);

        // Extrahiere den Namen und die Nachricht
        string name = jsonData["data"]["tags"]["display-name"].ToString();
        string message = jsonData["data"]["message"].ToString();

        // Gib den Namen und die Nachricht in der Unity-Konsole aus
        Debug.Log($"{name}: {message}");

        // Aktualisiere den Text im TextMeshPro-Objekt im Hauptthread
        UpdateTextOnMainThread(name, message);
    }

void UpdateTextOnMainThread(string name, string message)
{
    Debug.Log($"Updating text on main thread: {name}: {message}");

    // Use Unity's MainThreadDispatcher to update UI on the main thread
    UnityMainThreadDispatcher.Instance().Enqueue(() =>
    {
        tmpField.text = $"{name}: {message}";
    });
}


    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }
}
