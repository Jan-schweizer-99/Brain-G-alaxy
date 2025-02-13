using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ParentLightSwitch : MonoBehaviour
{
    [System.Serializable]
    public class LightSwitchInfo
    {
        public LightSwitch lightSwitch;
        public bool isLightOn;
    }

    public List<LightSwitchInfo> childLightSwitches = new List<LightSwitchInfo>();

    // Neue öffentliche Eigenschaft für die Anzahl der eingeschalteten Lichter
    public int NumberOfActiveLights
    {
        get { return childLightSwitches.Count(info => info.isLightOn); }
    }

    // Event-Deklaration für den Lichtwechsel
    public delegate void LightsChangedHandler();
    public event LightsChangedHandler OnLightsChanged;

#if UNITY_EDITOR
    [CustomEditor(typeof(ParentLightSwitch))]
    public class ParentLightSwitchEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ParentLightSwitch parentSwitch = (ParentLightSwitch)target;

            // Button to toggle lights in children
            if (GUILayout.Button("Toggle Lights in Children"))
            {
                parentSwitch.SetLightsStateInChildren(!parentSwitch.childLightSwitches[0].isLightOn);
            }

            // Button to randomly deactivate 5 active lights
            if (GUILayout.Button("Randomly Deactivate 5 Active Lights"))
            {
                parentSwitch.RandomlyDeactivateActiveLights(5);
            }

            // Anzeige der Anzahl der eingeschalteten Lichter
            EditorGUILayout.LabelField("Number of Active Lights: " + parentSwitch.NumberOfActiveLights);

            // Display list of child light switches with individual toggles
            EditorGUILayout.LabelField("Child Light Switches:");
            foreach (LightSwitchInfo childSwitchInfo in parentSwitch.childLightSwitches)
            {
                childSwitchInfo.isLightOn = EditorGUILayout.Toggle(childSwitchInfo.lightSwitch.gameObject.name, childSwitchInfo.isLightOn);
                childSwitchInfo.lightSwitch.SetLightState(childSwitchInfo.isLightOn);
            }

            // Trigger the event when the lights are changed
            if (GUI.changed && parentSwitch.OnLightsChanged != null)
            {
                parentSwitch.OnLightsChanged.Invoke();
            }
        }
    }
#endif

    void Awake()
    {
        InitializeChildLightSwitches();
    }

    private void InitializeChildLightSwitches()
    {
        childLightSwitches.Clear();

        // Find all child objects with LightSwitch components and sort by index
        LightSwitch[] childSwitches = GetComponentsInChildren<LightSwitch>(true)
            .OrderBy(child => child.transform.GetSiblingIndex())
            .ToArray();

        // Create LightSwitchInfo objects for each child switch
        foreach (LightSwitch childSwitch in childSwitches)
        {
            LightSwitchInfo switchInfo = new LightSwitchInfo
            {
                lightSwitch = childSwitch,
                isLightOn = childSwitch.isLightOn
            };
            childLightSwitches.Add(switchInfo);
        }
    }

    public void SetLightsStateInChildren(bool newState)
    {
        foreach (LightSwitchInfo childSwitchInfo in childLightSwitches)
        {
            childSwitchInfo.isLightOn = newState;
            childSwitchInfo.lightSwitch.SetLightState(newState);
        }

        // Trigger the event when the lights are changed
        OnLightsChanged?.Invoke();
    }

    public void RandomlyDeactivateActiveLights(int numberOfLightsToDeactivate)
    {
        // Filter the currently active lights
        List<LightSwitchInfo> activeLights = childLightSwitches.FindAll(l => l.isLightOn);

        // Deactivate exactly 5 randomly selected active lights
        for (int i = 0; i < numberOfLightsToDeactivate; i++)
        {
            if (activeLights.Count > 0)
            {
                int randomIndex = Random.Range(0, activeLights.Count);
                activeLights[randomIndex].isLightOn = false;
                activeLights[randomIndex].lightSwitch.SetLightState(false);
                activeLights.RemoveAt(randomIndex);
            }
            else
            {
                break; // Break if there are no more active lights to deactivate
            }
        }

        // Trigger the event when the lights are changed
        OnLightsChanged?.Invoke();
    }

    // New method to handle the child switch state change
    public void ChildSwitchStateChanged(LightSwitch childSwitch, bool newState)
    {
        // Find the corresponding LightSwitchInfo in the list and update its state
        LightSwitchInfo switchInfo = childLightSwitches.Find(info => info.lightSwitch == childSwitch);
        if (switchInfo != null)
        {
            switchInfo.isLightOn = newState;

            // Trigger the event when the lights are changed
            OnLightsChanged?.Invoke();
        }
    }

    // New method to register a child switch
    public void RegisterChildSwitch(LightSwitch childSwitch)
    {
        if (!childLightSwitches.Any(info => info.lightSwitch == childSwitch))
        {
            // Register the new child switch
            LightSwitchInfo switchInfo = new LightSwitchInfo
            {
                lightSwitch = childSwitch,
                isLightOn = childSwitch.isLightOn
            };
            childLightSwitches.Add(switchInfo);
        }
    }
}
