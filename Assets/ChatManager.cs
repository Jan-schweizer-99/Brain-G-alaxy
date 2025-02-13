/*#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public Transform chatPanel;
    public TextMeshProUGUI messagePrefab;
    public ScrollRect scrollRect;
    public int maxMessages = 10;

    private List<TextMeshProUGUI> messageList = new List<TextMeshProUGUI>();

    [System.Serializable]
    public class ChatMessage
    {
        public string name;
        public string message;
    }

    public List<ChatMessage> chatMessages = new List<ChatMessage>();

    private void Start()
    {
        UpdateMessageDisplay();
    }

    public void UpdateMessages()
    {
        // Überprüfe, ob die Anzahl der Chatnachrichten die Maximalanzahl überschreitet
        if (chatMessages.Count > maxMessages)
        {
            chatMessages.RemoveRange(0, chatMessages.Count - maxMessages);
        }

        // Aktualisiere die Anzeige der Nachrichten
        UpdateMessageDisplay();
    }

    private void UpdateMessageDisplay()
    {
        // Lösche vorhandene Nachrichten
        foreach (TextMeshProUGUI textElement in messageList)
        {
            Destroy(textElement.gameObject);
        }
        messageList.Clear();

        // Erstelle und zeige die Chatnachrichten an
        for (int i = chatMessages.Count - 1; i >= 0; i--)
        {
            CreateTextElement($"{chatMessages[i].name}: {chatMessages[i].message}");
        }

        // Aktualisiere die Scrollposition, um die neuesten Nachrichten anzuzeigen
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CreateTextElement(string text)
    {
        TextMeshProUGUI newText = Instantiate(messagePrefab, chatPanel);
        newText.text = text;

        messageList.Add(newText);
    }

#if UNITY_EDITOR
    // Diese Funktion aktualisiert die Anzeige im Editor, wenn sich die Liste ändert
    private void OnValidate()
    {
        UpdateMessages();
    }
#endif
}
*/