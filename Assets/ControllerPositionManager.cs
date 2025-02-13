// ControllerPositionManager.cs

using UnityEngine;
using System.Collections;

public class ControllerPositionManager : MonoBehaviour
{
    private GameObject leftController;
    private GameObject rightController;
    private GameObject point1Object;
    private bool positionPrintingEnabled = false;

    private float initialObjectYOffset;
    public float minYOffset = 0f;
    private float maxYValue;
    private float minYValue;

    GameObject TutorialColider;

    public float returnSpeed = 1f;
    private float normalizedPosition;

    private bool wasPositionPrintingEnabled = false;
    private bool isAnimating = false;

    public GameObject Dart;

    public float animationSpeed = 5f; // Einstellbarer Wert für die Animationsgeschwindigkeit

    public delegate void NormalizedPositionChangedDelegate(float normalizedPosition);
    public static event NormalizedPositionChangedDelegate OnNormalizedPositionChanged;

    private void Start()
    {
        TutorialColider = GameObject.FindGameObjectWithTag("Tutorial");
        FindControllersAndPoint1();
        maxYValue = point1Object.transform.position.y;
        minYValue = minYOffset + maxYValue;
    }

    private void FindControllersAndPoint1()
    {
        leftController = GameObject.FindGameObjectWithTag("LeftControllerTag");
        rightController = GameObject.FindGameObjectWithTag("RightControllerTag");

        if (leftController == null)
        {
            Debug.LogError("Linker Controller nicht gefunden. Bitte überprüfen Sie den Tag.");
        }

        if (rightController == null)
        {
            Debug.LogError("Rechter Controller nicht gefunden. Bitte überprüfen Sie den Tag.");
        }

        if (transform.parent != null)
        {
            point1Object = transform.parent.Find("Point_1")?.gameObject;
        }

        if (point1Object == null)
        {
            Debug.LogError("Parent-Objekt 'Point_1' nicht gefunden.");
        }

        if (point1Object != null)
        {
            initialObjectYOffset = transform.position.y - point1Object.transform.position.y;
        }
    }

    public float NormalizedPosition
    {
        get { return normalizedPosition; }
    }

    public void EnablePositionPrinting()
    {
        //;
        TutorialColider.SetActive(false);
        positionPrintingEnabled = true;
        Debug.Log("Position Printing aktiviert.");

    }

    public void DisablePositionPrinting()
    {
        positionPrintingEnabled = false;
        Debug.Log("Position Printing deaktiviert.");
    }

    public void SetYPositionFromNormalizedWithAnimation(float normalizedY)
    {
        SetYPositionFromNormalizedWithAnimation(normalizedY, animationSpeed);
        
    }

    public void SetYPositionFromNormalizedWithAnimation(float normalizedY, float customAnimationSpeed)
    {
        if (normalizedY >= 0f && normalizedY <= 1f)
        {
            if (isAnimating)
            {
                Debug.LogWarning("Eine Animation ist bereits aktiv. Bitte warten Sie, bis sie abgeschlossen ist.");
                return;
            }

            StartCoroutine(MoveToPointWithAnimation(normalizedY, customAnimationSpeed));
        }
        else
        {
            Debug.LogWarning("Normalisierter Y-Wert sollte zwischen 0 und 1 liegen.");
        }
    }

    private IEnumerator MoveToPointWithAnimation(float targetNormalizedY, float customAnimationSpeed)
    {
        isAnimating = true;

        float elapsedTime = 0f;
        float startNormalizedY = normalizedPosition;

        while (elapsedTime < 1f)
        {
            float newY = Mathf.Lerp(minYValue, maxYValue, Mathf.SmoothStep(startNormalizedY, targetNormalizedY, elapsedTime));

            point1Object.transform.position = new Vector3(
                point1Object.transform.position.x,
                newY,
                point1Object.transform.position.z
            );

            normalizedPosition = Mathf.InverseLerp(minYValue, maxYValue, newY);

            elapsedTime += Time.deltaTime * customAnimationSpeed;

            yield return null;
        }

        isAnimating = false;
    }

    private void Update()
    {   
        
           //Debug.Log("Normalized Position: " + normalizedPosition);
        if (leftController == null || rightController == null || point1Object == null)
        {
            Debug.LogError("Controller oder Parent-Objekt nicht gefunden. Stellen Sie sicher, dass die Tags und Namen korrekt sind.");
            return;
        }

        if (positionPrintingEnabled != wasPositionPrintingEnabled)
        {
            wasPositionPrintingEnabled = positionPrintingEnabled;

            if (!positionPrintingEnabled)
            {
                OnNormalizedPositionChanged?.Invoke(normalizedPosition);
                EventSystem.Instance.SetLastNormalizedPosition(normalizedPosition);
            }
        }

        if (positionPrintingEnabled)
        {
            Dart.SetActive(true);
            float objectYChange = transform.position.y;
            float clampedY = Mathf.Clamp(objectYChange - initialObjectYOffset, minYValue, maxYValue);
            point1Object.transform.position = new Vector3(
                point1Object.transform.position.x,
                clampedY,
                point1Object.transform.position.z
            );

            Vector3 point1ObjectPosition = point1Object.transform.position;
            Vector3 leftControllerPosition = leftController.transform.position;
            Vector3 rightControllerPosition = rightController.transform.position;

            normalizedPosition = Mathf.InverseLerp(minYValue, maxYValue, clampedY);
            Dart.transform.rotation = Quaternion.Euler(Dart.transform.rotation.eulerAngles.x, Dart.transform.rotation.eulerAngles.y, 360f * normalizedPosition);
        }
        else
        {
            Dart.SetActive(false);
            float targetY = maxYValue;
            float currentY = point1Object.transform.position.y;
            float newY = Mathf.MoveTowards(currentY, targetY, returnSpeed * Time.deltaTime);
            point1Object.transform.position = new Vector3(
                point1Object.transform.position.x,
                newY,
                point1Object.transform.position.z
            );

            normalizedPosition = Mathf.InverseLerp(minYValue, maxYValue, newY);
        }
    }
}