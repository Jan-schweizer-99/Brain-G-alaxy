using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OllamaUIDisplay : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private OllamaIntegration ollamaIntegration;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TMP_FontAsset mathFont;

    [SerializeField, HideInInspector]
    private string systemPrompt = "You are a helpful AI assistant.";
    
    [SerializeField, HideInInspector]
    private string userPrompt = "";

    private string instanceId;
    private MarkdownFormatter markdownFormatter;

    // Store event handlers as fields for later deregistration
    private OllamaIntegration.ResponseUpdateHandler responseUpdateHandler;
    private OllamaIntegration.GenerationHandler generationStartHandler;
    private OllamaIntegration.GenerationHandler generationCompleteHandler;

    // Add initialization flag
    private bool isInitialized = false;

#if UNITY_EDITOR
    [CustomEditor(typeof(OllamaUIDisplay))]
    public class OllamaUIDisplayEditor : CustomBaseEditor
    {
        private SerializedProperty ollamaIntegrationProp;
        private SerializedProperty outputTextProp;
        private SerializedProperty mathFontProp;
        private SerializedProperty systemPromptProp;
        private SerializedProperty userPromptProp;

        private void OnEnable()
        {
            SetEditorStyle("Deepseek");
            ollamaIntegrationProp = serializedObject.FindProperty("ollamaIntegration");
            outputTextProp = serializedObject.FindProperty("outputText");
            mathFontProp = serializedObject.FindProperty("mathFont");
            systemPromptProp = serializedObject.FindProperty("systemPrompt");
            userPromptProp = serializedObject.FindProperty("userPrompt");

            // Initialize the target script when the editor is enabled
            var script = (OllamaUIDisplay)target;
            if (!script.isInitialized)
            {
                script.EditorInitialize();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            var script = (OllamaUIDisplay)target;

            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Required References", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            if (script.ollamaIntegration == null)
            {
                EditorGUILayout.HelpBox("OllamaIntegration reference is required!", MessageType.Error);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("System Prompt", EditorStyles.boldLabel);
            systemPromptProp.stringValue = EditorGUILayout.TextArea(
                systemPromptProp.stringValue, GUILayout.Height(80));
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("User Prompt", EditorStyles.boldLabel);
            userPromptProp.stringValue = EditorGUILayout.TextArea(
                userPromptProp.stringValue, GUILayout.Height(80));
            
            EditorGUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = !script.ollamaIntegration?.isGenerating ?? false ? 
                    new Color(0.2f, 0.8f, 0.2f) : Color.gray;
                
                EditorGUI.BeginDisabledGroup(script.ollamaIntegration?.isGenerating ?? true);
                if (GUILayout.Button("GENERATE", GUILayout.Height(30)))
                {
                    script.GenerateFromInspector();
                }
                EditorGUI.EndDisabledGroup();

                GUI.backgroundColor = script.ollamaIntegration?.isGenerating ?? false ? 
                    new Color(0.8f, 0.2f, 0.2f) : Color.gray;
                
                EditorGUI.BeginDisabledGroup(!script.ollamaIntegration?.isGenerating ?? true);
                if (GUILayout.Button("STOP", GUILayout.Height(30)))
                {
                    script.StopGeneration();
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;
            }

            if (script.outputText != null && !string.IsNullOrEmpty(script.outputText.text))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Current Output:", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(script.outputText.text, GUILayout.Height(100));
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(script);
            }
        }
    }
#endif

    private void Awake()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        instanceId = System.Guid.NewGuid().ToString();
        
        if (ollamaIntegration == null)
        {
            Debug.LogError($"[{gameObject.name}] OllamaIntegration reference is missing!");
            enabled = false;
            return;
        }

        if (outputText == null)
        {
            Debug.LogError($"[{gameObject.name}] Output Text reference is missing!");
            enabled = false;
            return;
        }

        markdownFormatter = new MarkdownFormatter();

        if (mathFont != null)
        {
            outputText.font = mathFont;
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No math font assigned - some symbols might not display correctly!");
        }

        SetupEventHandlers();
        isInitialized = true;
    }

#if UNITY_EDITOR
    public void EditorInitialize()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }
#endif

    private void SetupEventHandlers()
    {
        // Create event handlers
        responseUpdateHandler = (response, targetId) => 
        {
            if (targetId == instanceId)
            {
                UpdateOutput(response);
            }
        };
        
        generationStartHandler = (targetId) => 
        {
            if (targetId == instanceId)
            {
                HandleGenerationStarted();
            }
        };
        
        generationCompleteHandler = (targetId) => 
        {
            if (targetId == instanceId)
            {
                HandleGenerationCompleted();
            }
        };

        // Register event handlers
        ollamaIntegration.OnResponseUpdated += responseUpdateHandler;
        ollamaIntegration.OnGenerationStarted += generationStartHandler;
        ollamaIntegration.OnGenerationCompleted += generationCompleteHandler;
    }

    private void HandleGenerationStarted()
    {
        if (outputText != null)
        {
            outputText.SetText("Generating...");
            outputText.ForceMeshUpdate();
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(outputText);
            }
            #endif
        }
    }

    private void HandleGenerationCompleted()
    {
        #if UNITY_EDITOR
        if (outputText != null)
        {
            EditorUtility.SetDirty(this);
        }
        #endif
    }

    public void StopGeneration()
    {
        if (ollamaIntegration != null)
        {
            ollamaIntegration.StopGeneration(instanceId);
        }
    }

    private void UpdateOutput(string response)
    {
        if (outputText == null) return;

        string cleanResponse = markdownFormatter.RemoveThinkingSections(response);
        if (string.IsNullOrEmpty(cleanResponse)) return;

        string formattedText = markdownFormatter.MarkdownToRichText(cleanResponse);
        
        outputText.SetText(formattedText);
        outputText.ForceMeshUpdate();

        #if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(outputText);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        #endif
    }

    public void GenerateFromInspector()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (ollamaIntegration == null || string.IsNullOrEmpty(userPrompt))
        {
            Debug.LogWarning("Cannot generate: Missing integration or empty prompt!");
            return;
        }

        ollamaIntegration.SetSystemPrompt(systemPrompt);
        ollamaIntegration.SetPrompt(userPrompt);
        ollamaIntegration.GenerateResponse(instanceId);
    }

    private void OnDestroy()
    {
        if (ollamaIntegration != null)
        {
            ollamaIntegration.OnResponseUpdated -= responseUpdateHandler;
            ollamaIntegration.OnGenerationStarted -= generationStartHandler;
            ollamaIntegration.OnGenerationCompleted -= generationCompleteHandler;
        }
    }
}