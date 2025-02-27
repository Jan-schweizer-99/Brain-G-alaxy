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

    // Methode zum Modifizieren des Template-Prefabs, um die ID zu setzen
private void SetupTemplatePrefabWithId(int topicId)
{
    // Finde das Template-Prefab aus dem IslandPrefabManager
    SerializedObject serializedManager = new SerializedObject(islandManager);
    SerializedProperty templatePrefabProperty = serializedManager.FindProperty("templatePrefab");
    
    if (templatePrefabProperty != null && templatePrefabProperty.objectReferenceValue != null)
    {
        GameObject templatePrefab = templatePrefabProperty.objectReferenceValue as GameObject;
        if (templatePrefab != null)
        {
            // Finde oder füge Island-Komponente hinzu
            Island islandComponent = templatePrefab.GetComponent<Island>();
            if (islandComponent == null)
            {
                // Füge Component hinzu, falls noch nicht vorhanden
                islandComponent = templatePrefab.AddComponent<Island>();
                Debug.Log("Island-Komponente zum Template-Prefab hinzugefügt");
            }
            
            // Setze die ID direkt auf dem Template
            islandComponent.topicId = topicId;
            EditorUtility.SetDirty(templatePrefab);
            AssetDatabase.SaveAssets();
        }
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
    SerializedProperty templatePrefabProperty = serializedManager.FindProperty("templatePrefab");

        // Prüfe, ob wir ein Template-Prefab haben
        GameObject templatePrefab = null;
        if (templatePrefabProperty != null && templatePrefabProperty.objectReferenceValue != null)
        {
            templatePrefab = templatePrefabProperty.objectReferenceValue as GameObject;
        }
        
        foreach (var topic in topicDatabase.topics)
        {
            // Setze die Nummer entsprechend der Topic ID
            prefabNumberProperty.intValue = topic.id;
            
            // Setze den Namen im SerializedObject
            newIslandNameProperty.stringValue = topic.topic;
            serializedManager.ApplyModifiedProperties();

            int currentTopicId = topic.id;
            string islandName = topic.topic;
            
            // Versuche ID direkt dem Template zuzuweisen vor dem Kopieren
            if (templatePrefab != null)
            {
                // Modifiziere das Prefab-Template direkt (vorübergehend)
                Island templateIsland = templatePrefab.GetComponent<Island>();
                if (templateIsland == null)
                {
                    templateIsland = templatePrefab.AddComponent<Island>();
                }
                templateIsland.topicId = currentTopicId;
                EditorUtility.SetDirty(templatePrefab);
            }
            
            // Kopiere das Prefab mit dem IslandPrefabManager
            islandManager.CopyPrefabToFolder();
            
            // Finde den korrekten Pfad zum erstellten Prefab
            string prefabPath = Path.Combine(islandManager.targetFolderPath, $"{islandName}.prefab");
            
            // Modifiziere das Prefab direkt mit SerializedObject
            EditorApplication.delayCall += () => {
                // Stelle sicher, dass der Asset-Datenbank vollständig aktualisiert ist
                AssetDatabase.Refresh();
                
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset != null)
                {
                    SerializedObject serializedPrefab = new SerializedObject(prefabAsset);
                    
                    // Finde die Island-Komponente
                    Island islandComponent = prefabAsset.GetComponent<Island>();
                    if (islandComponent != null)
                    {
                        // Verwende SerializedObject, um die Prefab-Asset-Eigenschaft zu ändern
                        SerializedObject serializedIsland = new SerializedObject(islandComponent);
                        SerializedProperty topicIdProperty = serializedIsland.FindProperty("_topicId");
                        
                        if (topicIdProperty != null)
                        {
                            topicIdProperty.intValue = currentTopicId;
                            serializedIsland.ApplyModifiedProperties();
                            EditorUtility.SetDirty(prefabAsset);
                            AssetDatabase.SaveAssets();
                            Debug.Log($"Topic ID {currentTopicId} wurde für Insel '{islandName}' gesetzt.");
                        }
                        else
                        {
                            Debug.LogError($"_topicId Property nicht in Island-Komponente gefunden: {islandName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Keine Island-Komponente auf der erstellten Insel '{islandName}' gefunden!");
                        
                        // Als Fallback: Versuche, Island-Komponente hinzuzufügen
                        islandComponent = prefabAsset.AddComponent<Island>();
                        if (islandComponent != null)
                        {
                            SerializedObject serializedIsland = new SerializedObject(islandComponent);
                            SerializedProperty topicIdProperty = serializedIsland.FindProperty("_topicId");
                            
                            if (topicIdProperty != null)
                            {
                                topicIdProperty.intValue = currentTopicId;
                                serializedIsland.ApplyModifiedProperties();
                                EditorUtility.SetDirty(prefabAsset);
                                AssetDatabase.SaveAssets();
                                Debug.Log($"Island-Komponente hinzugefügt und Topic ID {currentTopicId} gesetzt für '{islandName}'.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Konnte erstellte Insel nicht laden: {prefabPath}");
                }
            };
            
        }

        // Speichere alle Änderungen
        AssetDatabase.SaveAssets();

        Debug.Log($"Inseln wurden aus {topicDatabase.topics.Count} Topics erstellt!");

        // Aktualisiere jetzt direkt die IDs auf allen erstellten Inseln
        EditorApplication.delayCall += () => {
            // Stelle sicher, dass die Asset-Datenbank vollständig aktualisiert ist
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // Daten aus der TopicDatabase kopieren für schnelleren Zugriff
            Dictionary<string, int> topicIdsbyName = new Dictionary<string, int>();
            foreach (var topic in topicDatabase.topics)
            {
                topicIdsbyName[topic.topic] = topic.id;
                Debug.Log($"Topic-Mapping: '{topic.topic}' -> ID {topic.id}");
            }
            
            // Finde alle Prefabs im Zielordner
            if (!Directory.Exists(islandManager.targetFolderPath))
            {
                Debug.LogError($"Zielordner existiert nicht: {islandManager.targetFolderPath}");
                return;
            }
            
            string[] prefabFiles = Directory.GetFiles(islandManager.targetFolderPath, "*.prefab");
            Debug.Log($"Gefundene Prefabs: {prefabFiles.Length}");
            int updatedCount = 0;
            
            foreach (string prefabPath in prefabFiles)
            {
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                Debug.Log($"Verarbeite Prefab: {prefabName}");
                
                // Versuche die ID basierend auf dem exakten Namen zu finden
                if (topicIdsbyName.TryGetValue(prefabName, out int topicId))
                {
                    // Lade das Prefab
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefabAsset == null)
                    {
                        Debug.LogError($"Konnte Prefab nicht laden: {prefabPath}");
                        continue;
                    }
                    
                    // Finde Island-Komponente
                    Island islandComponent = prefabAsset.GetComponent<Island>();
                    if (islandComponent == null)
                    {
                        Debug.LogWarning($"Keine Island-Komponente auf {prefabName} gefunden, füge sie hinzu");
                        islandComponent = prefabAsset.AddComponent<Island>();
                    }
                    
                    // Benutze SerializedObject für korrekte Persistenz von Änderungen
                    SerializedObject serializedObject = new SerializedObject(islandComponent);
                    SerializedProperty topicIdProperty = serializedObject.FindProperty("_topicId");
                    
                    if (topicIdProperty != null)
                    {
                        Debug.Log($"Setze ID für {prefabName}: {topicId}");
                        topicIdProperty.intValue = topicId;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefabAsset);
                        updatedCount++;
                    }
                    else
                    {
                        Debug.LogError($"Konnte _topicId Property nicht finden in Island-Komponente: {prefabName}");
                    }
                }
                else
                {
                    // Versuche eine teilweise Übereinstimmung für den Fall, dass Präfixe oder andere Formatierungen verwendet werden
                    bool found = false;
                    foreach (var pair in topicIdsbyName)
                    {
                        // Wenn einer im anderen enthalten ist
                        if (prefabName.Contains(pair.Key) || pair.Key.Contains(prefabName))
                        {
                            Debug.Log($"Teilweise Übereinstimmung gefunden: {prefabName} ~ {pair.Key}");
                            
                            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            if (prefabAsset != null)
                            {
                                Island islandComponent = prefabAsset.GetComponent<Island>();
                                if (islandComponent == null)
                                {
                                    islandComponent = prefabAsset.AddComponent<Island>();
                                }
                                
                                SerializedObject serializedObject = new SerializedObject(islandComponent);
                                SerializedProperty topicIdProperty = serializedObject.FindProperty("_topicId");
                                
                                if (topicIdProperty != null)
                                {
                                    Debug.Log($"Setze ID für {prefabName}: {pair.Value} (teilweise Übereinstimmung mit {pair.Key})");
                                    topicIdProperty.intValue = pair.Value;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(prefabAsset);
                                    updatedCount++;
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (!found)
                    {
                        Debug.LogWarning($"Kein passendes Topic für Prefab gefunden: {prefabName}");
                    }
                }
            }
            
            // Speichere alle Änderungen
            AssetDatabase.SaveAssets();
            
            if (updatedCount > 0)
            {
                Debug.Log($"{updatedCount} Inseln wurden mit den korrekten Topic-IDs aktualisiert.");
            }
            else
            {
                Debug.LogWarning("Keine Inseln konnten aktualisiert werden!");
            }
            
            // Aktualisiere den Spiral Placer wenn vorhanden
            if (islandManager.spiralPlacer != null)
            {
                islandManager.spiralPlacer.RegenerateAll();
            }
        };
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