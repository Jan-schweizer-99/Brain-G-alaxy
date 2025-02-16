// TopicDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TopicEntry
{
    public int id;
    public string mainTopic;  // Neues Feld für das Hauptthema
    public string topic;
    public string section;
    public List<string> tags = new List<string>();
}

[CreateAssetMenu(fileName = "TopicDatabase", menuName = "ScriptableObjects/TopicDatabase")]
public class TopicDatabase : ScriptableObject
{
    public List<TopicEntry> topics = new List<TopicEntry>();
}