using UnityEngine;
using WebSocketSharp;

[System.Serializable]
public class ChatMessage
{
    public string userId;
    public string username;
    public string message;
    public string timestamp;
    public string modStatus;
    public string badges;
    public string subTier;
}

public class TwitchchatWS : MonoBehaviour
{
    private WebSocket webSocket;

    // Start is called before the first frame update
    void Start()
    {
        string serverAddress = "ws://localhost:8080"; // Ändere die Adresse entsprechend deinem Server

        webSocket = new WebSocket(serverAddress);
        webSocket.OnMessage += OnWebSocketMessage;

        webSocket.Connect();
        Debug.Log("Connected to server: " + serverAddress);
    }

    private void OnWebSocketMessage(object sender, MessageEventArgs e)
    {
        string jsonString = e.Data;
        Debug.Log("Received message from server: " + jsonString);

        ChatMessage chatMessage = JsonUtility.FromJson<ChatMessage>(jsonString);
        if (chatMessage != null)
        {
            // Hier kannst du die Chat-Nachricht verarbeiten und in der Unity-Konsole ausgeben
            Debug.Log($"User: {chatMessage.username} ({chatMessage.userId}), Mod Status: {chatMessage.modStatus}, Message: {chatMessage.message}");
        }
    }

    // Optional: Du kannst die Verbindung im OnDestroy oder OnApplicationQuit schließen.
    void OnDestroy()
    {
        if (webSocket != null && webSocket.IsAlive)
        {
            webSocket.Close();
        }
    }
}
