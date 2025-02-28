using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "TopicDatabase", menuName = "ScriptableObjects/TopicDatabase")]
public class TopicDatabase : ScriptableObject 
{
    public List<TopicEntry> topics = new List<TopicEntry>();
    
    // Hilfsmethoden zum Finden von Topics
    public TopicEntry GetTopicById(int id)
    {
        return topics.Find(t => t.id == id);
    }
    
    public List<TopicEntry> GetTopicsByMainTopic(string mainTopic)
    {
        return topics.FindAll(t => t.mainTopic == mainTopic);
    }
    
    public List<TopicEntry> GetTopicsBySection(string section)
    {
        return topics.FindAll(t => t.section == section);
    }
    
    public List<TopicEntry> GetTopicsByTag(string tag)
    {
        return topics.FindAll(t => t.tags.Contains(tag));
    }
    
    // Methode zum Überprüfen ob alle Topics Erklärungen haben
    public bool AllTopicsHaveExplanations()
    {
        return topics.TrueForAll(t => !string.IsNullOrEmpty(t.explanation));
    }
    
    // Methode zum Zählen der Topics mit Erklärungen
    public int CountTopicsWithExplanations()
    {
        return topics.Count(t => !string.IsNullOrEmpty(t.explanation));
    }
}