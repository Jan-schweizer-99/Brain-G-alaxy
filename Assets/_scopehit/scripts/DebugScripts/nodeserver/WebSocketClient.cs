using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(WebSocketClient))]
[CanEditMultipleObjects]
public class WebSocketClientEditor : CustomBaseEditor
{
        protected override void OnEnable()
    {
        SetEditorStyle("Network");
    }
}
#endif
[Serializable]
public class WebSocketEventMapping
{
    public string eventName;           // Der Name des Events aus JS
    public UnityEvent<string> action;  // Die Unity-Funktion(en) die ausgeführt werden sollen
}

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private bool isConnected = false;
    private bool isReconnecting = false;

    [SerializeField]
    private string _serverUrl = "ws://localhost:8080";
    public string serverUrl
    {
        get { return _serverUrl; }
        set 
        { 
            _serverUrl = value;
            if (ws != null)
            {
                ws.Close();
                ConnectToServer();
            }
        }
    }

    [Header("Connection Settings")]
    [SerializeField]
    private float reconnectDelay = 5f;  // Verzögerung in Sekunden vor Wiederverbindungsversuch

    [Header("Event Mappings")]
    [SerializeField]
    private List<WebSocketEventMapping> eventMappings = new List<WebSocketEventMapping>();

    // Dictionary für schnelleren Zugriff zur Laufzeit
    private Dictionary<string, UnityEvent<string>> eventDictionary;

    void Awake()
    {
        // Erstelle Dictionary aus den Mappings für schnelleren Zugriff
        eventDictionary = new Dictionary<string, UnityEvent<string>>();
        foreach (var mapping in eventMappings)
        {
            if (!string.IsNullOrEmpty(mapping.eventName))
            {
                eventDictionary[mapping.eventName] = mapping.action;
            }
        }
    }

    void Start()
    {
        // Verzögere den ersten Verbindungsversuch um 2 Sekunden
        // um sicherzustellen, dass der Server Zeit hat zu starten
        Invoke("ConnectToServer", 2f);
    }

    void ConnectToServer()
    {
        if (isReconnecting)
        {
            return;
        }

        if (ws != null)
        {
            ws.Close();
            ws = null;
        }

        try
        {
            ws = new WebSocket(serverUrl);

            ws.OnOpen += (sender, e) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log($"<color=#00FF00>[WebSocket] Verbindung hergestellt zu {serverUrl}!</color>");
                    isConnected = true;
                    isReconnecting = false;
                });
            };

            ws.OnMessage += (sender, e) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    HandleMessage(e.Data);
                });
            };

            ws.OnClose += (sender, e) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log($"<color=#FF9900>[WebSocket] Verbindung geschlossen! Code: {e.Code}, Grund: {e.Reason}</color>");
                    isConnected = false;

                    if (!isReconnecting)
                    {
                        isReconnecting = true;
                        Debug.Log($"<color=#FF9900>[WebSocket] Versuche Wiederverbindung in {reconnectDelay} Sekunden...</color>");
                        Invoke("ConnectToServer", reconnectDelay);
                    }
                });
            };

            ws.OnError += (sender, e) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.LogError($"<color=#FF0000>[WebSocket] Fehler: {e.Message}</color>");
                    if (ws != null)
                    {
                        ws.Close();
                    }
                });
            };

            Debug.Log($"<color=#00FF00>[WebSocket] Verbindungsversuch zu {serverUrl}...</color>");
            ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#FF0000>[WebSocket] Fehler beim Verbindungsaufbau: {ex.Message}</color>");
            if (!isReconnecting)
            {
                isReconnecting = true;
                Invoke("ConnectToServer", reconnectDelay);
            }
        }
    }

    void HandleMessage(string jsonMessage)
    {
        try
        {
            // Originale Nachricht loggen
            Debug.Log($"<color=#00FF00>[WebSocket] Rohe Nachricht empfangen:</color> {jsonMessage}");

            var message = JsonConvert.DeserializeObject<WebSocketMessage>(jsonMessage);
            
            // Geparste Nachricht detailliert loggen
            Debug.Log($"<color=#00FF00>[WebSocket] Event Details:</color>\n" +
                     $"  Event-Typ: {message.type}\n" +
                     $"  Event-Daten: {message.data}\n" +
                     $"  Zeitstempel: {DateTime.Now.ToString("HH:mm:ss.fff")}");
            
            if (eventDictionary.ContainsKey(message.type))
            {
                Debug.Log($"<color=#00FF00>[WebSocket] Führe Handler für Event '{message.type}' aus</color>");
                // Führe alle registrierten Funktionen für diesen Event aus
                eventDictionary[message.type].Invoke(message.data);
                
                // Zeige an, wie viele Listener das Event hat
                int listenerCount = eventDictionary[message.type].GetPersistentEventCount();
                Debug.Log($"<color=#00FF00>[WebSocket] Event '{message.type}' wurde an {listenerCount} Listener gesendet</color>");
            }
            else
            {
                Debug.LogWarning($"<color=#FFFF00>[WebSocket] Warnung: Kein Handler gefunden für Event: {message.type}</color>");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#FF0000>[WebSocket] Fehler beim Verarbeiten der Nachricht:</color>\n" +
                          $"  Nachricht: {jsonMessage}\n" +
                          $"  Fehler: {ex.Message}\n" +
                          $"  StackTrace: {ex.StackTrace}");
        }
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    // Optional: Methode zum manuellen Neustarten der Verbindung
    public void RestartConnection()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
        isReconnecting = false;
        ConnectToServer();
    }

    // Optional: Methode zum Hinzufügen von Events zur Laufzeit
    public void AddEventMapping(string eventName, UnityAction<string> action)
    {
        if (!eventDictionary.ContainsKey(eventName))
        {
            var newEvent = new UnityEvent<string>();
            eventDictionary[eventName] = newEvent;
        }
        eventDictionary[eventName].AddListener(action);
    }

    // Optional: Methode zum Prüfen des Verbindungsstatus
    public bool IsConnected()
    {
        return isConnected && ws != null && ws.ReadyState == WebSocketState.Open;
    }
}

[Serializable]
public class WebSocketMessage
{
    public string type;    // Der Event-Typ
    public string data;    // Die Event-Daten
}

