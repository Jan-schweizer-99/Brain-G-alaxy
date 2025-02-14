
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

[ExecuteInEditMode]
public class OllamaIntegration : MonoBehaviour
{
    [Header("Remote Server Configuration")]
    [Tooltip("URL des Python-Servers, z.B. http://192.168.1.100:5000/generate")]
    [SerializeField] private string apiUrl = "http://IP_ADRESS_SERVER:5000/generate";
    [SerializeField] private string modelName = "deepseek-r1:14b";

    private string prompt = "";
    private string suffix = "";
    private string systemPrompt = "You are a helpful AI assistant.";
    private string template = "";
    private bool streamResponse = true;
    private bool rawMode = false;
    private string responseFormat = "";
    private string keepAliveDuration = "5m";
    private float temperature = 0.7f;
    private float topP = 0.95f;
    private int numPredict = 2048;

    // Delegate-Definitionen mit Instance ID
    public delegate void ResponseUpdateHandler(string response, string instanceId);
    public delegate void GenerationHandler(string instanceId);

    // Events mit Instance ID
    public event ResponseUpdateHandler OnResponseUpdated;
    public event GenerationHandler OnGenerationStarted;
    public event GenerationHandler OnGenerationCompleted;

    private string currentInstanceId;
    
    [System.Serializable]
    private class ConversationItem
    {
        public string role;
        public string content;
        public List<int> tokenContext = new List<int>();

        public ConversationItem(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
    private int contextWindowSize = 3;
    private List<ConversationItem> contextHistory = new List<ConversationItem>();

    private List<int> lastContextNumbers = new List<int>();
    private string response = "";

    public bool isGenerating { get; private set; } = false;

    public void SetSystemPrompt(string newSystemPrompt)
    {
        systemPrompt = newSystemPrompt;
    }

    public void SetPrompt(string newPrompt)
    {
        prompt = newPrompt;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(OllamaIntegration))]
    public class OllamaIntegrationEditor : CustomBaseEditor
    {
        private bool showContextHistory = false;
        private bool showAdvancedSettings = false;
        private void OnEnable()
        {
            SetEditorStyle("Deepseek");// set style for custom Editor
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Dies ist wichtig für den Hintergrund und das Logo
            var script = (OllamaIntegration)target;

            EditorGUI.BeginDisabledGroup(script.isGenerating);

            EditorGUILayout.LabelField("Remote Server Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Prompt Settings", EditorStyles.boldLabel);
            script.prompt = EditorGUILayout.TextArea(script.prompt, GUILayout.Height(60));

            EditorGUILayout.Space(10);
            
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                script.systemPrompt = EditorGUILayout.TextArea(script.systemPrompt, GUILayout.Height(60));
                script.responseFormat = EditorGUILayout.TextField("Response Format", script.responseFormat);
                script.keepAliveDuration = EditorGUILayout.TextField("Keep Alive Duration", script.keepAliveDuration);
                script.rawMode = EditorGUILayout.Toggle("Raw Mode", script.rawMode);
                script.streamResponse = EditorGUILayout.Toggle("Stream Response", script.streamResponse);
                
                EditorGUILayout.Space(5);
                
                script.temperature = EditorGUILayout.Slider("Temperature", script.temperature, 0f, 2f);
                script.topP = EditorGUILayout.Slider("Top P", script.topP, 0f, 1f);
                script.numPredict = EditorGUILayout.IntSlider("Max Tokens", script.numPredict, 1, 4096);
                script.contextWindowSize = EditorGUILayout.IntSlider("Context Window Size", script.contextWindowSize, 0, 10);
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            
            showContextHistory = EditorGUILayout.Foldout(showContextHistory, "Context History");
            if (showContextHistory)
            {
                EditorGUI.indentLevel++;
                foreach (var item in script.contextHistory)
                {
                    EditorGUILayout.LabelField($"{item.role}:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(item.content, EditorStyles.wordWrappedLabel);
                }
                if (GUILayout.Button("Clear Context History"))
                {
                    script.contextHistory.Clear();
                    script.lastContextNumbers.Clear();
                    EditorUtility.SetDirty(script);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Response", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(script.response, GUILayout.Height(100));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                EditorGUI.BeginDisabledGroup(script.isGenerating);
                if (GUILayout.Button("Generate Response", GUILayout.Height(30)))
                {
                    script.GenerateInInspector();
                }
                EditorGUI.EndDisabledGroup();
                
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                EditorGUI.BeginDisabledGroup(!script.isGenerating);
                if (GUILayout.Button("Stop Generation", GUILayout.Height(30)))
                {
                    script.isGenerating = false;
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(script);
            }
        }
    }

    public void GenerateInInspector()
    {
        string editorInstanceId = "editor_" + System.Guid.NewGuid().ToString();
        if (!isGenerating && !string.IsNullOrEmpty(prompt))
        {
            if (!EditorApplication.isPlaying)
            {
                EditorCoroutineUtility.StartCoroutine(GenerateResponseCoroutine(editorInstanceId), this);
            }
            else
            {
                StartCoroutine(GenerateResponseCoroutine(editorInstanceId));
            }
        }
        else if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("Please enter a prompt before generating.");
        }
    }
#endif

    private void UpdateContextHistory(string newPrompt, string newResponse)
    {
        if (contextWindowSize > 0)
        {
            newPrompt = CleanThinkContent(newPrompt);
            newResponse = CleanThinkContent(newResponse);

            contextHistory.Add(new ConversationItem("user", newPrompt));
            contextHistory.Add(new ConversationItem("assistant", newResponse));
            
            while (contextHistory.Count > contextWindowSize * 2)
            {
                contextHistory.RemoveAt(0);
                contextHistory.RemoveAt(0);
            }
        }
    }

    private string CleanThinkContent(string text)
    {
        while (true)
        {
            int startIndex = text.IndexOf("<think>", System.StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) break;

            int endIndex = text.IndexOf("</think>", startIndex, System.StringComparison.OrdinalIgnoreCase);
            if (endIndex == -1) break;

            text = text.Remove(startIndex, (endIndex + 8) - startIndex);
            text = text.Trim();
        }
        return text;
    }

    public void GenerateResponse(string instanceId)
    {
        if (!isGenerating && !string.IsNullOrEmpty(prompt))
        {
            StartCoroutine(GenerateResponseCoroutine(instanceId));
        }
    }

    public void StopGeneration(string instanceId)
    {
        if (currentInstanceId == instanceId)
        {
            isGenerating = false;
            OnGenerationCompleted?.Invoke(instanceId);
            currentInstanceId = null;
        }
    }

    private IEnumerator GenerateResponseCoroutine(string instanceId)
    {
        currentInstanceId = instanceId;
        isGenerating = true;
        OnGenerationStarted?.Invoke(instanceId);
        response = "Generating...";
        OnResponseUpdated?.Invoke(response, instanceId);

        var payload = new Dictionary<string, object>
        {
            { "model", modelName },
            { "prompt", prompt }
        };

        if (!string.IsNullOrEmpty(suffix))
            payload["suffix"] = suffix;

        if (!string.IsNullOrEmpty(responseFormat))
            payload["format"] = responseFormat;

        var options = new Dictionary<string, object>
        {
            { "temperature", temperature },
            { "top_p", topP },
            { "num_predict", numPredict }
        };
        payload["options"] = options;

        if (!string.IsNullOrEmpty(systemPrompt) && !rawMode)
            payload["system"] = systemPrompt;

        if (!string.IsNullOrEmpty(template) && !rawMode)
            payload["template"] = template;

        payload["stream"] = streamResponse;
        
        if (rawMode)
            payload["raw"] = true;

        if (!string.IsNullOrEmpty(keepAliveDuration))
            payload["keep_alive"] = keepAliveDuration;

        if (contextWindowSize > 0 && lastContextNumbers.Count > 0 && !rawMode)
        {
            payload["context"] = lastContextNumbers;
        }

        string jsonPayload = JsonConvert.SerializeObject(payload);
        string responseData = "";

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 3600;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                responseData = request.downloadHandler.text;
                Debug.Log($"Raw response: {responseData}");
            }
            else
            {
                Debug.LogError($"Request failed: {request.error}");
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    Debug.LogError($"Error response: {request.downloadHandler.text}");
                }
                response = $"Error: {request.error}";
                OnResponseUpdated?.Invoke(response, instanceId);
                isGenerating = false;
                OnGenerationCompleted?.Invoke(instanceId);
                currentInstanceId = null;
                yield break;
            }
        }

        StringBuilder fullResponse = new StringBuilder();

        if (streamResponse)
        {
            string[] chunks = responseData.Split('\n');

            foreach (string chunk in chunks)
            {
                if (!isGenerating)
                {
                    response = "Generation stopped by user.";
                    OnResponseUpdated?.Invoke(response, instanceId);
                    break;
                }

                if (string.IsNullOrEmpty(chunk)) continue;

                try
                {
                    var chunkData = JsonConvert.DeserializeObject<OllamaResponse>(chunk);
                    if (chunkData != null && !string.IsNullOrEmpty(chunkData.response))
                    {
                        fullResponse.Append(chunkData.response);
                        response = fullResponse.ToString();
                        OnResponseUpdated?.Invoke(response, instanceId);
                        #if UNITY_EDITOR
                        if (!EditorApplication.isPlaying)
                        {
                            EditorUtility.SetDirty(this);
                        }
                        #endif
                    }
                }
                catch (JsonException e)
                {
                    Debug.LogError($"Failed to parse chunk: {chunk}\nError: {e.Message}");
                }
                yield return null;
            }
        }
        else
        {
            try
            {
                var singleResponse = JsonConvert.DeserializeObject<OllamaResponse>(responseData);
                if (singleResponse != null)
                {
                    fullResponse.Append(singleResponse.response);
                    response = fullResponse.ToString();
                    OnResponseUpdated?.Invoke(response, instanceId);
                    
                    if (singleResponse.context != null)
                    {
                        lastContextNumbers = singleResponse.context;
                    }
                    
                    #if UNITY_EDITOR
                    if (!EditorApplication.isPlaying)
                    {
                        EditorUtility.SetDirty(this);
                    }
                    #endif
                }
            }
            catch (JsonException e)
            {
                Debug.LogError($"Failed to parse response: {responseData}\nError: {e.Message}");
            }
        }

        if (!rawMode)
        {
            UpdateContextHistory(prompt, fullResponse.ToString());
        }
        
        isGenerating = false;
        OnGenerationCompleted?.Invoke(instanceId);
        currentInstanceId = null;
    }

    private class OllamaResponse
    {
        public string response { get; set; }
        public bool done { get; set; }
        public List<int> context { get; set; }
    }
}
