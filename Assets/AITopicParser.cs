using UnityEngine;
using TMPro;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[ExecuteInEditMode]
public class AITopicParser : MonoBehaviour 
{
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TopicDatabase topicDatabase;
    
    [Header("Island Generation")]
    [SerializeField] private IslandPrefabManager islandManager;
    
    private string mainTopic = "";

    private void OnValidate()
    {
        if (islandManager == null)
        {
            islandManager = FindObjectOfType<IslandPrefabManager>();
        }
    }

    public void ParseAIContent()
    {
        if (contentText == null || topicDatabase == null)
        {
            Debug.LogError("Fehlende Referenzen zum TMP_Text oder TopicDatabase!");
            return;
        }

        string fullText = contentText.text;
        
        // Finde den Content nach dem </think> tag
        int thinkEndIndex = fullText.IndexOf("</think>");
        if (thinkEndIndex == -1)
        {
            Debug.LogWarning("Kein </think> Tag gefunden. Verarbeite gesamten Text.");
            thinkEndIndex = 0;
        }
        else
        {
            thinkEndIndex += 8; // Länge von "</think>"
        }
        
        string contentToProcess = fullText.Substring(thinkEndIndex).Trim();
        
        // Lösche existierende Topics
        topicDatabase.topics.Clear();
        
        // Verarbeite jede Zeile
        string[] lines = contentToProcess.Split('\n');
        bool isFirstTopic = true;
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("!"))
            {
                if (isFirstTopic)
                {
                    // Extrahiere das Hauptthema aus der ersten Zeile
                    Match firstTopicMatch = Regex.Match(trimmedLine, @"!(\d+)-([^$#]+)");
                    if (firstTopicMatch.Success)
                    {
                        mainTopic = firstTopicMatch.Groups[2].Value.Trim();
                        isFirstTopic = false;
                    }
                }
                ProcessTopicLine(trimmedLine);
            }
        }
        
        // Speichere Änderungen
        EditorUtility.SetDirty(topicDatabase);
        AssetDatabase.SaveAssets();
        Debug.Log($"KI-generierte Topics erfolgreich gespeichert! Hauptthema: {mainTopic}");
        Debug.Log($"Anzahl verarbeiteter Topics: {topicDatabase.topics.Count}");
    }
    
    private void ProcessTopicLine(string line)
    {
        TopicEntry entry = new TopicEntry();
        
        try 
        {
            // Setze das Hauptthema für jeden Eintrag
            entry.mainTopic = mainTopic;

            // Extrahiere ID und Topic
            Match idTopicMatch = Regex.Match(line, @"!(\d+)-([^$#]+)");
            if (idTopicMatch.Success)
            {
                // Konvertiere ID zu int
                if (int.TryParse(idTopicMatch.Groups[1].Value, out int idNumber))
                {
                    entry.id = idNumber;
                }
                else
                {
                    Debug.LogError($"Konnte ID nicht zu Zahl konvertieren: {idTopicMatch.Groups[1].Value}");
                    return;
                }
                entry.topic = idTopicMatch.Groups[2].Value.Trim();
            }
            else
            {
                Debug.LogWarning($"Konnte keine ID/Topic in Zeile finden: {line}");
                return;
            }
            
            // Extrahiere Section
            Match sectionMatch = Regex.Match(line, @"\$([^#]+)");
            if (sectionMatch.Success)
            {
                entry.section = sectionMatch.Groups[1].Value.Trim();
            }
            else
            {
                Debug.LogWarning($"Konnte keine Section in Zeile finden: {line}");
                return;
            }
            
            // Extrahiere Tags
            entry.tags = new List<string>();
            MatchCollection tagMatches = Regex.Matches(line, @"#(\w+)");
            foreach (Match tagMatch in tagMatches)
            {
                entry.tags.Add(tagMatch.Groups[1].Value);
            }
            
            // Füge nur valide Einträge hinzu
            topicDatabase.topics.Add(entry);
            Debug.Log($"Topic hinzugefügt: ID={entry.id}, MainTopic={entry.mainTopic}, Topic={entry.topic}, Section={entry.section}, Tags={string.Join(", ", entry.tags)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fehler beim Verarbeiten der Zeile: {line}\nError: {e.Message}");
        }
    }

    public void DeleteAllIslands()
    {
        if (islandManager == null)
        {
            Debug.LogError("Kein IslandPrefabManager gefunden!");
            return;
        }

        string[] existingFiles = Directory.GetFiles(islandManager.targetFolderPath, "*.prefab");
        foreach (string file in existingFiles)
        {
            AssetDatabase.DeleteAsset(file);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Alle Inseln wurden gelöscht!");

        // Aktualisiere den Spiral Placer wenn vorhanden
        if (islandManager.spiralPlacer != null)
        {
            EditorApplication.delayCall += () => {
                islandManager.spiralPlacer.RegenerateAll();
            };
        }
    }

    public void CreateIslandsFromTopics()
    {
        if (islandManager == null)
        {
            Debug.LogError("Kein IslandPrefabManager gefunden!");
            return;
        }

        if (topicDatabase == null || topicDatabase.topics.Count == 0)
        {
            Debug.LogError("Keine Topics zum Erstellen von Inseln gefunden!");
            return;
        }

        // Durchlaufe alle Topics und erstelle Inseln
        SerializedObject serializedManager = new SerializedObject(islandManager);
        SerializedProperty newIslandNameProperty = serializedManager.FindProperty("newIslandName");
        SerializedProperty prefabNumberProperty = serializedManager.FindProperty("prefabNumber");

        foreach (var topic in topicDatabase.topics)
        {
            // Setze die Nummer entsprechend der Topic ID
            prefabNumberProperty.intValue = topic.id;
            
            // Setze den Namen im SerializedObject
            newIslandNameProperty.stringValue = topic.topic;
            serializedManager.ApplyModifiedProperties();

            // Kopiere das Prefab mit dem IslandPrefabManager
            islandManager.CopyPrefabToFolder();
        }

        Debug.Log($"Inseln wurden aus {topicDatabase.topics.Count} Topics erstellt!");

        // Aktualisiere den Spiral Placer wenn vorhanden
        if (islandManager.spiralPlacer != null)
        {
            EditorApplication.delayCall += () => {
                islandManager.spiralPlacer.RegenerateAll();
            };
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AITopicParser))]
public class AITopicParserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        AITopicParser parser = (AITopicParser)target;
        
        EditorGUILayout.Space(10);

        if (GUILayout.Button("Parse AI Content"))
        {
            parser.ParseAIContent();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Insel Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Alle Inseln Löschen"))
        {
            if (EditorUtility.DisplayDialog("Inseln Löschen",
                "Möchten Sie wirklich alle Inseln löschen?",
                "Ja", "Abbrechen"))
            {
                parser.DeleteAllIslands();
            }
        }

        if (GUILayout.Button("Inseln aus Topics Erstellen"))
        {
            parser.CreateIslandsFromTopics();
        }
    }
}
#endif