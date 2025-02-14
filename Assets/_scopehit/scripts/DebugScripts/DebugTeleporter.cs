using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugTeleporter : MonoBehaviour
{
    [System.Serializable]
    public class ObjectTeleportData
    {
        public GameObject targetObject;
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public bool useLocalSpace = true;
        public Transform referenceTransform;

        public Vector3 GetTargetWorldPosition()
        {
            if (!useLocalSpace)
                return targetPosition;

            if (referenceTransform != null)
            {
                Matrix4x4 referenceLocalToWorld = referenceTransform.localToWorldMatrix;
                Vector3 positionInReferenceSpace = referenceLocalToWorld.MultiplyPoint3x4(targetPosition);
                return positionInReferenceSpace;
            }

            return targetObject.transform.parent != null 
                ? targetObject.transform.parent.TransformPoint(targetPosition)
                : targetPosition;
        }

        public Quaternion GetTargetWorldRotation()
        {
            if (!useLocalSpace)
                return Quaternion.Euler(targetRotation);

            if (referenceTransform != null)
            {
                Quaternion targetRot = Quaternion.Euler(targetRotation);
                return referenceTransform.localToWorldMatrix.rotation * targetRot;
            }

            return targetObject.transform.parent != null
                ? targetObject.transform.parent.rotation * Quaternion.Euler(targetRotation)
                : Quaternion.Euler(targetRotation);
        }

        public Vector3 GetPositionRelativeToReference()
        {
            if (referenceTransform == null)
                return targetPosition;

            Matrix4x4 worldToReferenceLocal = referenceTransform.worldToLocalMatrix;
            return worldToReferenceLocal.MultiplyPoint3x4(targetObject.transform.position);
        }

        public Vector3 GetRotationRelativeToReference()
        {
            if (referenceTransform == null)
                return targetRotation;

            Quaternion relativeRot = Quaternion.Inverse(referenceTransform.rotation) * targetObject.transform.rotation;
            return relativeRot.eulerAngles;
        }
    }

    [System.Serializable]
    public class TeleportStep
    {
        [Header("Setup")]
        public string interactionName = "New Interaction";
        public List<ObjectTeleportData> objectsData = new List<ObjectTeleportData>();
        
        [Header("Timing")]
        public float duration = 3f;
        [Tooltip("Wartezeit bevor der nächste Schritt ausgeführt wird")]
        public float delayBeforeNextStep = 0f;
        
        [Header("Options")]
        public bool permanent = false;
    }

    [SerializeField]
    private List<TeleportStep> steps = new List<TeleportStep>();
    
    private Dictionary<GameObject, TransformData> originalTransforms = new Dictionary<GameObject, TransformData>();
    private Coroutine sequenceCoroutine;
    private bool isPlaying = false;

    private class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public bool wasLocal;
        public Transform referenceTransform;
    }

    public void ExecuteStep(int stepIndex)
    {
        if (isPlaying || stepIndex < 0 || stepIndex >= steps.Count) return;
        StartCoroutine(ExecuteSingleStep(steps[stepIndex]));
    }

    private IEnumerator ExecuteSingleStep(TeleportStep step)
    {
        isPlaying = true;
        originalTransforms.Clear();

        foreach (var objData in step.objectsData)
        {
            if (objData.targetObject != null)
            {
                Transform objTransform = objData.targetObject.transform;

                // Originalposition speichern
                if (!step.permanent)
                {
                    originalTransforms[objData.targetObject] = new TransformData
                    {
                        position = objTransform.position,
                        rotation = objTransform.rotation,
                        localPosition = objTransform.localPosition,
                        localRotation = objTransform.localRotation,
                        wasLocal = objData.useLocalSpace,
                        referenceTransform = objData.referenceTransform
                    };
                }

                if (objData.useLocalSpace && objData.referenceTransform != null)
                {
                    // Berechne die finale Weltposition basierend auf der Referenz-Hierarchie
                    Vector3 finalWorldPos = objData.GetTargetWorldPosition();
                    Quaternion finalWorldRot = objData.GetTargetWorldRotation();

                    objTransform.SetPositionAndRotation(finalWorldPos, finalWorldRot);
                }
                else if (objData.useLocalSpace)
                {
                    objTransform.localPosition = objData.targetPosition;
                    objTransform.localRotation = Quaternion.Euler(objData.targetRotation);
                }
                else
                {
                    objTransform.position = objData.targetPosition;
                    objTransform.rotation = Quaternion.Euler(objData.targetRotation);
                }
            }
        }

        if (!step.permanent)
        {
            yield return new WaitForSeconds(step.duration);
            ResetAllObjects();

            if (step.delayBeforeNextStep > 0)
            {
                yield return new WaitForSeconds(step.delayBeforeNextStep);
            }
        }

        isPlaying = false;
    }

    private void ResetAllObjects()
    {
        foreach (var kvp in originalTransforms)
        {
            if (kvp.Key != null)
            {
                Transform objTransform = kvp.Key.transform;
                TransformData originalData = kvp.Value;

                if (originalData.wasLocal && originalData.referenceTransform != null)
                {
                    // Stelle die ursprüngliche Position relativ zum Referenzobjekt wieder her
                    objTransform.position = originalData.position;
                    objTransform.rotation = originalData.rotation;
                }
                else if (originalData.wasLocal)
                {
                    objTransform.localPosition = originalData.localPosition;
                    objTransform.localRotation = originalData.localRotation;
                }
                else
                {
                    objTransform.position = originalData.position;
                    objTransform.rotation = originalData.rotation;
                }
            }
        }
        originalTransforms.Clear();
    }

    private void OnDisable()
    {
        if (isPlaying)
        {
            ResetAllObjects();
            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }
            isPlaying = false;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DebugTeleporter))]
public class DebugTeleporterEditor : CustomBaseEditor
{
    private GUIStyle buttonStyle;
    
    protected override float AdditionalBackgroundHeight => 330f;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        SetEditorStyle("DEBUG");
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null) return;
        serializedObject.Update();

        // Prüfen ob der CustomEditor deaktiviert ist
        var editorReference = GetEditorReference();
        bool isCustomEditorDisabled = editorReference != null && editorReference.DisableCustomEditor;

        if (isCustomEditorDisabled || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Wenn deaktiviert, nur den Standard Inspector zeichnen
            DrawDefaultInspector();
        }
        else 
        {
            // Wenn aktiviert, Custom Editor mit Hintergrund und Logo zeichnen
            DrawCustomBackground();
            DrawLogo();
            DrawDefaultInspector();
        }

        // Debug Controls werden immer gezeichnet
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.padding = new RectOffset(10, 10, 6, 6);
            buttonStyle.margin = new RectOffset(0, 0, 10, 2);
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Buttons are disabled in Edit Mode", MessageType.Info);
        }

        DebugTeleporter teleporter = (DebugTeleporter)target;
        SerializedProperty stepsProperty = serializedObject.FindProperty("steps");

        using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
        {
            for (int i = 0; i < stepsProperty.arraySize; i++)
            {
                var stepProperty = stepsProperty.GetArrayElementAtIndex(i);
                var nameProperty = stepProperty.FindPropertyRelative("interactionName");

                if (GUILayout.Button(nameProperty.stringValue, buttonStyle))
                {
                    teleporter.ExecuteStep(i);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif