using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
[CustomEditor(typeof(UnityMainThreadDispatcher))]
public class UnityMainThreadDispatcherEditor : CustomBaseEditor 
{
    protected override void OnEnable()
    {
        SetEditorStyle("Network");
    }
}
#endif
public class UnityMainThreadDispatcher : MonoBehaviour 
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            throw new Exception("UnityMainThreadDispatcher not initialized. Call Init() first.");
        }
        return _instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
    }

    public void OnDestroy()
    {
        _instance = null;
    }

    public void Update()
    {
        while (_executionQueue.Count > 0)
        {
            _executionQueue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }
}

