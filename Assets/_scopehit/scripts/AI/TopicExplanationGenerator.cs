using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[ExecuteInEditMode]
public class TopicExplanationGenerator : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] public TopicDatabase topicDatabase;
    [SerializeField] private OllamaUIDisplay ollamaDisplay;
    [SerializeField] private OllamaIntegration ollamaIntegration;

    [Header("Generation Settings")]
    [SerializeField, TextArea(5, 8)] private string systemPrompt = "Du bist ein Lehrer. Deine Aufgabe ist es, komplexe Themen für Schüler verständlich zu erklären. Verwende eine klare, altersgerechte Sprache und baue gerne passende Beispiele ein.";
    [SerializeField, TextArea(3, 5)] private string explanationPromptTemplate = "Bitte erkläre das folgende Thema für Schüler: {0}\nDas Thema gehört zum Hauptthema: {1}\nUnd zur Sektion: {2}\nWichtige Tags sind: {3}";
    
    public bool isGenerating { get; private set; } = false;
    private int currentTopicIndex = 0;
    private bool shouldStop = false;
    private int selectedTopicIndex = -1; // -1 bedeutet "Alle Topics"

    private void OnValidate()
    {
        if (ollamaDisplay == null)
        {
            ollamaDisplay = FindObjectOfType<OllamaUIDisplay>();
        }
        if (ollamaIntegration == null && ollamaDisplay != null)
        {
            ollamaIntegration = ollamaDisplay.GetComponent<OllamaIntegration>();
        }
    }

    public void StartExplanationGeneration(int topicIndex = -1)
    {
        if (isGenerating)
        {
            Debug.LogWarning("Already generating explanations!");
            return;
        }

        if (topicDatabase == null || ollamaDisplay == null || ollamaIntegration == null)
        {
            Debug.LogError("Missing required references!");
            return;
        }

        if (topicDatabase.topics.Count == 0)
        {
            Debug.LogWarning("No topics found in database!");
            return;
        }

        // Wenn ein spezifisches Topic gewählt wurde
        if (topicIndex >= 0 && topicIndex < topicDatabase.topics.Count)
        {
            selectedTopicIndex = topicIndex;
            currentTopicIndex = topicIndex;
        }
        else
        {
            selectedTopicIndex = -1;
            currentTopicIndex = 0;
        }

        isGenerating = true;
        shouldStop = false;
        
        // Registriere Event Handler
        ollamaIntegration.OnGenerationCompleted += HandleGenerationComplete;
        
        GenerateNextExplanation();
        
        Debug.Log($"Started explanation generation process... {(selectedTopicIndex >= 0 ? $"für Topic: {topicDatabase.topics[selectedTopicIndex].topic}" : "für alle Topics")}");
    }

    public void StopExplanationGeneration()
    {
        if (!isGenerating) return;
        
        shouldStop = true;
        ollamaDisplay.StopGeneration();
        Debug.Log("Stopping explanation generation...");
    }

    void GenerateNextExplanation()
    {
        // Wenn ein einzelnes Topic generiert wird
        if (selectedTopicIndex >= 0)
        {
            if (!isGenerating || shouldStop)
            {
                CleanupAndFinish();
                return;
            }
            GenerateExplanationForTopic(selectedTopicIndex);
        }
        // Wenn alle Topics generiert werden
        else
        {
            if (!isGenerating || shouldStop || currentTopicIndex >= topicDatabase.topics.Count)
            {
                CleanupAndFinish();
                return;
            }
            GenerateExplanationForTopic(currentTopicIndex);
        }
    }

    void GenerateExplanationForTopic(int index)
    {
        TopicEntry currentTopic = topicDatabase.topics[index];
        
        // Erstelle den Prompt
        string prompt = string.Format(explanationPromptTemplate,
            currentTopic.topic,
            currentTopic.mainTopic,
            currentTopic.section,
            string.Join(", ", currentTopic.tags));

        // Setze den Prompt und generiere
        if (ollamaDisplay != null)
        {
            var serializedDisplay = new SerializedObject(ollamaDisplay);
            
            // Setze System Prompt
            var systemPromptProperty = serializedDisplay.FindProperty("systemPrompt");
            systemPromptProperty.stringValue = systemPrompt;
            
            // Setze User Prompt
            var userPromptProperty = serializedDisplay.FindProperty("userPrompt");
            userPromptProperty.stringValue = prompt;
            serializedDisplay.ApplyModifiedProperties();
            
            ollamaDisplay.GenerateFromInspector();
            
            if (selectedTopicIndex >= 0)
            {
                Debug.Log($"Generating explanation for topic: {currentTopic.topic}");
            }
            else
            {
                Debug.Log($"Generating explanation for topic {currentTopicIndex + 1}/{topicDatabase.topics.Count}: {currentTopic.topic}");
            }
        }
    }

    void HandleGenerationComplete(string targetId)
    {
        if (!isGenerating || shouldStop) return;

        // Speichere die generierte Erklärung
        if (ollamaDisplay != null)
        {
            var serializedDisplay = new SerializedObject(ollamaDisplay);
            var outputTextProperty = serializedDisplay.FindProperty("outputText");
            
            if (outputTextProperty != null && outputTextProperty.objectReferenceValue != null)
            {
                var tmpText = outputTextProperty.objectReferenceValue as TextMeshProUGUI;
                if (tmpText != null)
                {
                    // Remove <think></think> tags from the text
                    string cleanedText = System.Text.RegularExpressions.Regex.Replace(
                        tmpText.text,
                        @"<think>.*?</think>",
                        string.Empty,
                        System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                    
                    int currentIndex = selectedTopicIndex >= 0 ? selectedTopicIndex : currentTopicIndex;
                    topicDatabase.topics[currentIndex].explanation = cleanedText;
                    
                    // Markiere das Database Asset als verändert
                    EditorUtility.SetDirty(topicDatabase);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        // Wenn ein einzelnes Topic generiert wurde, sind wir fertig
        if (selectedTopicIndex >= 0)
        {
            CleanupAndFinish();
            return;
        }

        // Sonst zum nächsten Topic
        currentTopicIndex++;
        
        // Starte die nächste Generation nach einer kurzen Pause
        EditorApplication.delayCall += () => {
            GenerateNextExplanation();
        };
    }

    void CleanupAndFinish()
    {
        isGenerating = false;
        shouldStop = false;
        currentTopicIndex = 0;
        selectedTopicIndex = -1;
        
        // Deregistriere Event Handler
        if (ollamaIntegration != null)
        {
            ollamaIntegration.OnGenerationCompleted -= HandleGenerationComplete;
        }
        
        Debug.Log("Explanation generation completed!");
        
        // Save any pending changes
        EditorUtility.SetDirty(topicDatabase);
        AssetDatabase.SaveAssets();
    }

    private void OnDestroy()
    {
        if (ollamaIntegration != null)
        {
            ollamaIntegration.OnGenerationCompleted -= HandleGenerationComplete;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TopicExplanationGenerator))]
public class TopicExplanationGeneratorEditor : Editor
{
    private int selectedTopicIndex = -1; // -1 = Alle Topics
    private bool showTopicSelection = false;
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        var generator = (TopicExplanationGenerator)target;
        var serializedObject = new SerializedObject(generator);
        
        // Zeichne Properties, aber überspringe systemPrompt
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // Überspringe das systemPrompt Feld, da wir es später custom zeichnen
            if (iterator.name == "systemPrompt")
                continue;
                
            EditorGUILayout.PropertyField(iterator, true);
        }
        
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUILayout.Space(10);
        
        // System Prompt anzeigen und bearbeiten
        EditorGUILayout.LabelField("System Prompt", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(generator.isGenerating);
        var systemPromptProperty = serializedObject.FindProperty("systemPrompt");
        EditorGUILayout.PropertyField(systemPromptProperty, GUIContent.none, GUILayout.Height(100));
        if (serializedObject.hasModifiedProperties)
        {
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.Space(10);
        
        // Topic Auswahl
        showTopicSelection = EditorGUILayout.Foldout(showTopicSelection, "Topic Auswahl");
        if (showTopicSelection && generator.topicDatabase != null)
        {
            EditorGUI.indentLevel++;
            
            // "Alle Topics" Option
            bool allTopicsSelected = selectedTopicIndex == -1;
            bool newAllTopicsSelected = EditorGUILayout.ToggleLeft("Alle Topics", allTopicsSelected);
            if (newAllTopicsSelected != allTopicsSelected)
            {
                selectedTopicIndex = newAllTopicsSelected ? -1 : 0;
            }

            if (!newAllTopicsSelected && generator.topicDatabase.topics.Count > 0)
            {
                // Scrollbare Liste für einzelne Topics
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
                
                for (int i = 0; i < generator.topicDatabase.topics.Count; i++)
                {
                    var topic = generator.topicDatabase.topics[i];
                    bool isSelected = selectedTopicIndex == i;
                    bool newIsSelected = EditorGUILayout.ToggleLeft(
                        $"{topic.topic} (ID: {topic.id})", isSelected);
                    
                    if (newIsSelected && !isSelected)
                    {
                        selectedTopicIndex = i;
                    }
                    else if (!newIsSelected && isSelected)
                    {
                        selectedTopicIndex = -1;
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        
        // Buttons
        using (new EditorGUILayout.HorizontalScope())
        {
            string buttonText = (selectedTopicIndex >= 0 && generator.topicDatabase != null && 
                                generator.topicDatabase.topics.Count > selectedTopicIndex) ? 
                $"Generate '{generator.topicDatabase.topics[selectedTopicIndex].topic}'" : 
                "Generate All Explanations";

            EditorGUI.BeginDisabledGroup(generator.isGenerating || generator.topicDatabase == null || 
                                         (generator.topicDatabase != null && generator.topicDatabase.topics.Count == 0));
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                generator.StartExplanationGeneration(selectedTopicIndex);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(!generator.isGenerating);
            if (GUILayout.Button("Cancel Generation", GUILayout.Height(30)))
            {
                generator.StopExplanationGeneration();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif