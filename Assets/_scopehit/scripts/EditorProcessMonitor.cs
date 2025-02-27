using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

public class EditorProcessMonitor : EditorWindow
{
    [MenuItem("Tools/Editor Process Monitor")]
    public static void ShowWindow()
    {
        GetWindow<EditorProcessMonitor>("Process Monitor");
    }

    private List<EditorCallData> callData = new List<EditorCallData>();
    private double lastUpdateTime;
    private const double UPDATE_INTERVAL = 0.5; // Update every 0.5 seconds
    private bool isMonitoring = false;
    private Vector2 scrollPosition;
    private int maxCallsToShow = 20;
    private bool showAllCalls = false;
    private string searchFilter = "";
    
    private class EditorCallData
    {
        public string methodName;
        public double executionTime;
        public System.DateTime timestamp;
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Editor Process Monitor", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isMonitoring ? "Stop Monitoring" : "Start Monitoring"))
        {
            isMonitoring = !isMonitoring;
            if (isMonitoring)
            {
                EditorApplication.update += MonitorUpdate;
            }
            else
            {
                EditorApplication.update -= MonitorUpdate;
            }
        }
        
        if (GUILayout.Button("Clear Data"))
        {
            callData.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        searchFilter = EditorGUILayout.TextField("Filter", searchFilter);
        showAllCalls = EditorGUILayout.Toggle("Show All Calls", showAllCalls);
        
        if (!showAllCalls)
        {
            maxCallsToShow = EditorGUILayout.IntSlider("Max Calls to Show", maxCallsToShow, 1, 100);
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("Recent Editor Calls (with execution time in ms):", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Filter and display the call data
        List<EditorCallData> filteredCalls = callData;
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filteredCalls = callData.FindAll(call => 
                call.methodName.ToLower().Contains(searchFilter.ToLower()));
        }
        
        int callsToDisplay = showAllCalls ? filteredCalls.Count : Mathf.Min(filteredCalls.Count, maxCallsToShow);
        for (int i = 0; i < callsToDisplay; i++)
        {
            int index = filteredCalls.Count - 1 - i;
            if (index >= 0 && index < filteredCalls.Count)
            {
                EditorCallData call = filteredCalls[index];
                
                // Color code based on execution time
                GUI.color = call.executionTime < 10 ? Color.green : 
                           (call.executionTime < 50 ? Color.yellow : Color.red);
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{call.methodName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{call.executionTime:F2} ms", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                GUI.color = Color.white;
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void MonitorUpdate()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime < UPDATE_INTERVAL)
        {
            return;
        }
        
        lastUpdateTime = currentTime;
        
        // Get stack trace
        StackTrace stackTrace = new StackTrace(true);
        StringBuilder traceBuilder = new StringBuilder();
        
        Stopwatch sw = new Stopwatch();
        
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            
            // Skip system and unity editor methods
            if (method.DeclaringType.Namespace != null &&
                (method.DeclaringType.Namespace.StartsWith("System") ||
                 method.DeclaringType.Namespace.StartsWith("UnityEditor") ||
                 method.DeclaringType.Namespace.StartsWith("UnityEngine")))
            {
                continue;
            }
            
            sw.Start();
            
            // Try to invoke the method again to measure performance
            try
            {
                if (method.IsStatic)
                {
                    // Only try to invoke parameterless methods for simplicity
                    if (method.GetParameters().Length == 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore failures - we're just probing
            }
            
            sw.Stop();
            
            EditorCallData data = new EditorCallData
            {
                methodName = $"{method.DeclaringType.Name}.{method.Name}",
                executionTime = sw.Elapsed.TotalMilliseconds,
                timestamp = System.DateTime.Now
            };
            
            callData.Add(data);
            
            // Keep the list size reasonable
            if (callData.Count > 1000)
            {
                callData.RemoveAt(0);
            }
            
            sw.Reset();
        }
        
        Repaint();
    }
    
    private void OnDestroy()
    {
        if (isMonitoring)
        {
            EditorApplication.update -= MonitorUpdate;
        }
    }
}