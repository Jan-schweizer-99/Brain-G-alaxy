using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IslandNavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 2f;
    [Tooltip("Local offset from island position (X = right, Y = up, Z = forward relative to island rotation)")]
    public Vector3 viewOffset = new Vector3(0f, 2f, 0f);
    public float transitionDuration = 1f;
    
    [Header("Input Actions")]
    public InputActionReference nextIslandAction;
    public InputActionReference previousIslandAction;
    public InputActionReference selectIslandAction;
    public InputActionReference toggleFlyAction;
    
    [Header("References")]
    public ConicalSpiralIslandPlacer spiralPlacer;
    public XROrigin xrOrigin;
    public ActionBasedContinuousMoveProvider continuousMoveProvider;
    
    private Transform[] islandPositions;
    [SerializeField]
    private int currentIslandIndex = 0;
    public int CurrentIslandIndex => currentIslandIndex;
    
    private bool isTransitioning = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float transitionTime = 0f;

    private bool isFlightTransitioning = false;
    private float flightTransitionTime = 0f;
    private Vector3 flightTransitionStartPos;
    private float flightTransitionDuration = 0.5f;
    private bool isTransitioningToFlight;

    private void OnEnable()
    {
        if (spiralPlacer != null)
        {
            spiralPlacer.OnIslandsGenerated += RefreshIslands;
        }
    }

    private void OnDisable()
    {
        if (spiralPlacer != null)
        {
            spiralPlacer.OnIslandsGenerated -= RefreshIslands;
        }

        DisableInputActions();
    }
    
    private void Start()
    {
        EnableInputActions();
        RefreshIslands();
    }

    public void RefreshIslands()
    {
        if (spiralPlacer == null) return;

        // Sammle alle Inselpositionen
        List<(GameObject obj, int index)> validIslands = new List<(GameObject, int)>();
        
        for (int i = 0; i < spiralPlacer.transform.childCount; i++)
        {
            Transform child = spiralPlacer.transform.GetChild(i);
            string name = child.name;
            
            // Ignoriere spezielle Objekte
            if (name.Equals("SpiralMesh") || 
                name.Equals("SpiralParticleSystem") || 
                name.Equals("RoomOfNumbers_Center"))
            {
                continue;
            }
            
            // Extrahiere die Nummer aus dem Namen (Format: "XX-name")
            if (name.Length >= 2 && int.TryParse(name.Substring(0, 2), out int index))
            {
                validIslands.Add((child.gameObject, index));
            }
        }
        
        // Sortiere nach der Nummer
        var sortedIslands = validIslands.OrderBy(x => x.index).ToList();
        
        // Erstelle das finale Array
        islandPositions = new Transform[sortedIslands.Count];
        for (int i = 0; i < sortedIslands.Count; i++)
        {
            islandPositions[i] = sortedIslands[i].obj.transform;
        }
        
        if (islandPositions.Length > 0 && !isTransitioning && !isFlightTransitioning)
        {
            // Setze den Index zurück falls er außerhalb des gültigen Bereichs ist
            currentIslandIndex = Mathf.Clamp(currentIslandIndex, 0, islandPositions.Length - 1);
            
            if (xrOrigin != null)
            {
                // Aktiviere Flugmodus
                if (continuousMoveProvider != null)
                {
                    continuousMoveProvider.enableFly = true;
                    continuousMoveProvider.useGravity = false;
                }
                
                // Setze Position nur beim ersten Start
                if (!Application.isPlaying)
                {
                    Vector3 firstIslandPos = islandPositions[0].position;
                    Vector3 rotatedOffset = islandPositions[0].TransformDirection(new Vector3(5f, viewOffset.y, 5f));
                    xrOrigin.transform.position = firstIslandPos + rotatedOffset;
                    StartTransition(0);
                }
            }
        }
    }
    
    private void EnableInputActions()
    {
        if (nextIslandAction != null && nextIslandAction.action != null)
        {
            nextIslandAction.action.performed += OnNextIsland;
            nextIslandAction.action.Enable();
        }
        
        if (previousIslandAction != null && previousIslandAction.action != null)
        {
            previousIslandAction.action.performed += OnPreviousIsland;
            previousIslandAction.action.Enable();
        }
        
        if (selectIslandAction != null && selectIslandAction.action != null)
        {
            selectIslandAction.action.performed += OnSelectIsland;
            selectIslandAction.action.Enable();
        }
        
        if (toggleFlyAction != null && toggleFlyAction.action != null)
        {
            toggleFlyAction.action.performed += OnToggleFly;
            toggleFlyAction.action.Enable();
        }
    }

    private void DisableInputActions()
    {
        if (nextIslandAction != null && nextIslandAction.action != null)
        {
            nextIslandAction.action.performed -= OnNextIsland;
            nextIslandAction.action.Disable();
        }
        
        if (previousIslandAction != null && previousIslandAction.action != null)
        {
            previousIslandAction.action.performed -= OnPreviousIsland;
            previousIslandAction.action.Disable();
        }
        
        if (selectIslandAction != null && selectIslandAction.action != null)
        {
            selectIslandAction.action.performed -= OnSelectIsland;
            selectIslandAction.action.Disable();
        }
        
        if (toggleFlyAction != null && toggleFlyAction.action != null)
        {
            toggleFlyAction.action.performed -= OnToggleFly;
            toggleFlyAction.action.Disable();
        }
    }
    
    public void OnNextIsland(InputAction.CallbackContext context)
    {
        if (!isTransitioning && !isFlightTransitioning && 
            islandPositions != null && currentIslandIndex < islandPositions.Length - 1)
        {
            StartTransition(currentIslandIndex + 1);
        }
    }
    
    public void OnPreviousIsland(InputAction.CallbackContext context)
    {
        if (!isTransitioning && !isFlightTransitioning && 
            islandPositions != null && currentIslandIndex > 0)
        {
            StartTransition(currentIslandIndex - 1);
        }
    }
    
    public void OnSelectIsland(InputAction.CallbackContext context)
    {
        if (!isTransitioning && !isFlightTransitioning && !continuousMoveProvider.enableFly && 
            islandPositions != null && currentIslandIndex < islandPositions.Length)
        {
            Debug.Log($"Level {currentIslandIndex + 1} selected!");
            // Hier Ihre Level-Load-Logik implementieren
        }
    }
    
    public void OnToggleFly(InputAction.CallbackContext context)
    {
        if (!isTransitioning && !isFlightTransitioning && 
            continuousMoveProvider != null && islandPositions != null && 
            currentIslandIndex < islandPositions.Length)
        {
            bool newFlyState = !continuousMoveProvider.enableFly;
            
            // Starte die Transition
            isFlightTransitioning = true;
            flightTransitionTime = 0f;
            flightTransitionStartPos = xrOrigin.transform.position;
            isTransitioningToFlight = newFlyState;

            // Setze die Bewegungseinstellungen
            continuousMoveProvider.enableFly = newFlyState;
            continuousMoveProvider.useGravity = !newFlyState;
        }
    }
    
    private void StartTransition(int targetIndex)
    {
        if (isFlightTransitioning || islandPositions == null || 
            targetIndex < 0 || targetIndex >= islandPositions.Length) return;

        startPosition = xrOrigin.transform.position;
        startRotation = xrOrigin.transform.rotation;
        currentIslandIndex = targetIndex;
        isTransitioning = true;
        transitionTime = 0f;
    }
    
    private void Update()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            OnPreviousIsland(new InputAction.CallbackContext());
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            OnSelectIsland(new InputAction.CallbackContext());
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            OnNextIsland(new InputAction.CallbackContext());
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnToggleFly(new InputAction.CallbackContext());
        }
        #endif

        if (isTransitioning)
        {
            HandleTransition();
        }

        if (isFlightTransitioning)
        {
            HandleFlightTransition();
        }
    }
    
    private void HandleTransition()
    {
        if (islandPositions == null || currentIslandIndex >= islandPositions.Length)
        {
            isTransitioning = false;
            return;
        }

        transitionTime += Time.deltaTime;
        float t = transitionTime / transitionDuration;
        
        if (t >= 1f)
        {
            t = 1f;
            isTransitioning = false;
        }
        
        t = t * t * (3f - 2f * t);  // Smooth-Step
        
        // Zielposition mit rotiertem Offset
        Transform targetIsland = islandPositions[currentIslandIndex];
        Vector3 targetPosition = targetIsland.position;
        if (continuousMoveProvider.enableFly)
        {
            // Berechne den Offset relativ zur Zielinselrotation
            Vector3 rotatedOffset = targetIsland.TransformDirection(viewOffset);
            targetPosition += rotatedOffset;
        }
        
        Vector3 directionToCenter = Vector3.zero - new Vector3(targetPosition.x, 0f, targetPosition.z);
        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        
        xrOrigin.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        xrOrigin.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
    }

    private void HandleFlightTransition()
    {
        if (islandPositions == null || currentIslandIndex >= islandPositions.Length)
        {
            isFlightTransitioning = false;
            return;
        }

        flightTransitionTime += Time.deltaTime;
        float t = flightTransitionTime / flightTransitionDuration;
        
        if (t >= 1f)
        {
            t = 1f;
            isFlightTransitioning = false;
        }
        
        t = t * t * (3f - 2f * t); // Smooth-Step
        
        Transform currentIsland = islandPositions[currentIslandIndex];
        
        // Berechne den vollen und reduzierten Offset
        Vector3 fullOffset = currentIsland.TransformDirection(viewOffset);
        Vector3 reducedOffset = currentIsland.TransformDirection(new Vector3(0f, viewOffset.y, 0f)); // Nur Y-Offset beibehalten
        
        if (isTransitioningToFlight)
        {
            // Von reduziertem Offset (nur Y) zum vollen Offset
            Vector3 startPos = currentIsland.position + reducedOffset;
            Vector3 targetPos = currentIsland.position + fullOffset;
            xrOrigin.transform.position = Vector3.Lerp(startPos, targetPos, t);
        }
        else
        {
            // Vom vollen Offset zum reduzierten Offset (nur Y)
            Vector3 startPos = currentIsland.position + fullOffset;
            Vector3 targetPos = currentIsland.position + reducedOffset;
            xrOrigin.transform.position = Vector3.Lerp(startPos, targetPos, t);
        }
        
        // Optional: Auch die Blickrichtung anpassen
        Vector3 directionToCenter = Vector3.zero - new Vector3(xrOrigin.transform.position.x, 0f, xrOrigin.transform.position.z);
        xrOrigin.transform.rotation = Quaternion.LookRotation(directionToCenter);
    }
    
    public void MoveToIsland(int index)
    {
        if (islandPositions == null) return;
        
        if (index >= 0 && index < islandPositions.Length && !isFlightTransitioning)
        {
            currentIslandIndex = index;
            Transform targetIsland = islandPositions[currentIslandIndex];
            Vector3 targetPosition = targetIsland.position;
            if (continuousMoveProvider.enableFly)
            {
                // Berechne den Offset relativ zur Inselrotation
                Vector3 rotatedOffset = targetIsland.TransformDirection(viewOffset);
                targetPosition += rotatedOffset;
            }
            
            xrOrigin.transform.position = targetPosition;
            
            Vector3 directionToCenter = Vector3.zero - new Vector3(targetPosition.x, 0f, targetPosition.z);
            xrOrigin.transform.rotation = Quaternion.LookRotation(directionToCenter);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(IslandNavigator))]
public class IslandNavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IslandNavigator navigator = (IslandNavigator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug Navigation", EditorStyles.boldLabel);

        if (navigator.spiralPlacer != null && navigator.xrOrigin != null)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Previous Island"))
            {
                navigator.OnPreviousIsland(new InputAction.CallbackContext());
            }

            if (GUILayout.Button("Next Island"))
            {
                navigator.OnNextIsland(new InputAction.CallbackContext());
            }
            
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Toggle Flight Mode"))
            {
                navigator.OnToggleFly(new InputAction.CallbackContext());
            }

            if (GUILayout.Button("Select Current Island"))
            {
                navigator.OnSelectIsland(new InputAction.CallbackContext());
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Current Island Index: {navigator.CurrentIslandIndex}");
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign Spiral Placer and XR Origin to enable debug navigation.", MessageType.Warning);
        }
    }
}
#endif