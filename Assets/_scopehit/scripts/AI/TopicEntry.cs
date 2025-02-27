using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TopicEntry 
{
    public int id;
    public string mainTopic;
    public string topic;
    public string section;
    public List<string> tags = new List<string>();
    
    [TextArea(3, 10)]
    public string explanation;
}