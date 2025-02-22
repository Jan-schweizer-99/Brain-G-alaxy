using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PrefabSaver : MonoBehaviour
{
    // Leere MonoBehaviour-Klasse als Container für den Editor
}

#if UNITY_EDITOR
[CustomEditor(typeof(PrefabSaver))]
public class PrefabSaverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Standard Inspector zeichnen
        DrawDefaultInspector();

        // Referenz auf PrefabSaver Component
        PrefabSaver prefabSaver = (PrefabSaver)target;

        // Prüfen ob wir in einem Prefab sind
        if (PrefabUtility.IsPartOfPrefabInstance(prefabSaver.gameObject))
        {
            EditorGUILayout.Space(10);
            
            // Speicher-Button anzeigen
            if (GUILayout.Button("Prefab Änderungen speichern", GUILayout.Height(30)))
            {
                SavePrefabChanges(prefabSaver.gameObject);
            }
        }
    }

    private void SavePrefabChanges(GameObject targetObject)
    {
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject);
        if (prefabRoot != null)
        {
            // Alle Komponenten als dirty markieren
            var components = prefabRoot.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component != null)
                {
                    EditorUtility.SetDirty(component);
                }
            }

            // Prefab aktualisieren
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            
            // Szene als verändert markieren
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(targetObject.scene);
            }

            // Änderungen speichern
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Prefab '{prefabRoot.name}' wurde gespeichert!");
        }
    }
}
#endif