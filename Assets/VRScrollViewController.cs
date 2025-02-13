using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class VRScrollViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform handleArea;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionProperty gripAction; // Reference to the grip action
    [SerializeField] private float gripThreshold = 0.1f; // Minimum grip value to activate
    
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSensitivity = 1.0f;
    [SerializeField] private bool invertScroll = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color debugColorNoInteraction = Color.red;
    [SerializeField] private Color debugColorInteracting = Color.green;
    
    // Debug information
    private bool isLeftHandFound = false;
    private bool isRightHandFound = false;
    private bool isHandTouchingHandle = false;
    private bool isHandGripping = false;
    private Vector3 lastHandPosition3D;
    
    private bool isDragging = false;
    private Vector2 lastHandPosition;
    private XRDirectInteractor activeHand;

    // Debug GUI style
    private GUIStyle debugTextStyle;
    
    private void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
            
        // Ensure we have all necessary components
        if (content == null)
            content = scrollRect.content;
        if (viewport == null)
            viewport = scrollRect.viewport;
        if (handleArea == null)
            Debug.LogError("Handle area not assigned to VR Scroll View Controller!");

        // Enable the grip action
        gripAction.action.Enable();

        // Initialize debug text style
        debugTextStyle = new GUIStyle();
        debugTextStyle.normal.textColor = Color.white;
        debugTextStyle.fontSize = 14;
        debugTextStyle.fontStyle = FontStyle.Bold;
        
        // Try to find XR controllers in the scene
        FindXRControllers();
    }

    private void FindXRControllers()
    {
        var controllers = FindObjectsOfType<XRDirectInteractor>();
        foreach (var controller in controllers)
        {
            if (controller.gameObject.name.ToLower().Contains("left"))
            {
                isLeftHandFound = true;
                Debug.Log("Left VR controller found: " + controller.gameObject.name);
            }
            else if (controller.gameObject.name.ToLower().Contains("right"))
            {
                isRightHandFound = true;
                Debug.Log("Right VR controller found: " + controller.gameObject.name);
            }
        }

        if (!isLeftHandFound || !isRightHandFound)
        {
            Debug.LogWarning("Not all VR controllers were found in the scene!");
        }
    }
    
    public void OnHandEnterHandleArea(XRDirectInteractor hand)
    {
        isHandTouchingHandle = true;
        activeHand = hand;
        lastHandPosition = GetHandScreenPosition(hand);
        lastHandPosition3D = hand.transform.position;
        
        Debug.Log($"Hand entered handle area: {hand.gameObject.name}");
    }
    
    public void OnHandExitHandleArea(XRDirectInteractor hand)
    {
        if (hand == activeHand)
        {
            isHandTouchingHandle = false;
            if (isDragging)
            {
                isDragging = false;
                isHandGripping = false;
            }
            activeHand = null;
            
            Debug.Log($"Hand exited handle area: {hand.gameObject.name}");
        }
    }
    
    private void Update()
    {
        // Check grip input only when hand is touching handle
        if (isHandTouchingHandle && activeHand != null)
        {
            float gripValue = gripAction.action.ReadValue<float>();
            isHandGripping = gripValue > gripThreshold;

            // Start dragging only when gripping
            if (isHandGripping && !isDragging)
            {
                isDragging = true;
                lastHandPosition = GetHandScreenPosition(activeHand);
            }
            // Stop dragging when grip is released
            else if (!isHandGripping && isDragging)
            {
                isDragging = false;
            }
        }

        // Process scrolling only when dragging
        if (isDragging && isHandGripping && activeHand != null)
        {
            Vector2 currentHandPosition = GetHandScreenPosition(activeHand);
            Vector2 delta = currentHandPosition - lastHandPosition;
            
            // Calculate scroll movement
            float scrollDelta = invertScroll ? -delta.y : delta.y;
            
            // Apply scrolling
            float newVerticalNormalizedPosition = scrollRect.verticalNormalizedPosition + 
                (scrollDelta * scrollSensitivity * Time.deltaTime);
            
            // Clamp the value between 0 and 1
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newVerticalNormalizedPosition);
            
            lastHandPosition = currentHandPosition;
            lastHandPosition3D = activeHand.transform.position;

            // Log significant movement for debugging
            if (delta.magnitude > 0.1f)
            {
                Debug.Log($"Hand movement delta: {delta}, Scroll position: {scrollRect.verticalNormalizedPosition}");
            }
        }
    }
    
    private Vector2 GetHandScreenPosition(XRDirectInteractor hand)
    {
        // Convert hand position to screen space
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(hand.transform.position);
        
        // Convert to local position relative to handle area
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handleArea, 
            screenPoint, 
            Camera.main, 
            out Vector2 localPoint
        );
        
        return localPoint;
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        int yPos = 10;
        int xPos = 10;
        int lineHeight = 20;

        // Display controller status
        GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
            $"Left Controller: {(isLeftHandFound ? "Found ✓" : "Not Found ✗")}", 
            debugTextStyle);
        
        yPos += lineHeight;
        GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
            $"Right Controller: {(isRightHandFound ? "Found ✓" : "Not Found ✗")}", 
            debugTextStyle);
        
        yPos += lineHeight;
        GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
            $"Hand Touching Handle: {(isHandTouchingHandle ? "Yes ✓" : "No ✗")}", 
            debugTextStyle);

        yPos += lineHeight;
        GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
            $"Hand Gripping: {(isHandGripping ? "Yes ✓" : "No ✗")}", 
            debugTextStyle);

        if (isDragging && activeHand != null)
        {
            yPos += lineHeight;
            GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
                $"Active Hand: {activeHand.gameObject.name}", 
                debugTextStyle);
            
            yPos += lineHeight;
            GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
                $"Hand Position: {lastHandPosition3D:F2}", 
                debugTextStyle);
            
            yPos += lineHeight;
            GUI.Label(new Rect(xPos, yPos, 300, lineHeight), 
                $"Scroll Position: {scrollRect.verticalNormalizedPosition:F3}", 
                debugTextStyle);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || handleArea == null) return;

        // Draw handle area bounds
        Gizmos.color = isHandTouchingHandle ? 
            (isHandGripping ? debugColorInteracting : Color.yellow) : 
            debugColorNoInteraction;
            
        Vector3[] corners = new Vector3[4];
        handleArea.GetWorldCorners(corners);
        
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // Draw active hand position if dragging
        if (isDragging && activeHand != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastHandPosition3D, 0.05f);
        }
    }
    
    private void OnDestroy()
    {
        // Disable the grip action when the component is destroyed
        if (gripAction.action != null)
        {
            gripAction.action.Disable();
        }
    }
    
    // Optional: Add horizontal scrolling support
    public void EnableHorizontalScroll(bool enable)
    {
        scrollRect.horizontal = enable;
    }
    
    // Optional: Add vertical scrolling support
    public void EnableVerticalScroll(bool enable)
    {
        scrollRect.vertical = enable;
    }
}