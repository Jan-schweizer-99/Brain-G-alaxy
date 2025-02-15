#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

[ExecuteAlways]
public class TMPTextPersist : MonoBehaviour
{
    private TMP_Text tmpText;
    private SerializedObject serializedObject;
    private SerializedProperty textProperty;

    private void OnEnable()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            serializedObject = new SerializedObject(tmpText);
            textProperty = serializedObject.FindProperty("m_text");
            
            // Registriere für Play Mode Änderungen
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && tmpText != null)
        {
            // Speichere den Text in EditorPrefs
            string key = "TMPText_" + tmpText.GetInstanceID();
            string currentText = tmpText.text;
            EditorPrefs.SetString(key, currentText);
            Debug.Log($"Saving text: {currentText}");
        }
        else if (state == PlayModeStateChange.EnteredEditMode && tmpText != null)
        {
            // Hole den gespeicherten Text
            string key = "TMPText_" + tmpText.GetInstanceID();
            if (EditorPrefs.HasKey(key))
            {
                string savedText = EditorPrefs.GetString(key);
                
                // Aktualisiere den Text über SerializedProperty
                serializedObject.Update();
                textProperty.stringValue = savedText;
                serializedObject.ApplyModifiedProperties();
                
                // Markiere als verändert
                EditorUtility.SetDirty(tmpText);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                
                Debug.Log($"Restored text: {savedText}");
            }
        }
    }
}
#endif