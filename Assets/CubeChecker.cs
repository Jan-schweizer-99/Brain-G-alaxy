using UnityEngine;

public class CubeChecker : MonoBehaviour
{
    // Setze den Namen des Cubes, den du überprüfen möchtest
    public string cubeName = "Cube";

    private GameObject cube;

    void Start()
    {
        // Versuche, das GameObject mit dem angegebenen Namen zu finden
        cube = GameObject.Find(cubeName);
        if (cube == null)
        {
            Debug.LogError("Cube mit dem Namen '" + cubeName + "' wurde nicht gefunden.");
        }
        else
        {
            Debug.Log("Cube mit dem Namen '" + cubeName + "' wurde erfolgreich gefunden.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == cube)
        {
            Debug.Log("Cube entered the collider.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject == cube)
        {
            Debug.Log("Cube is inside the collider.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == cube)
        {
            Debug.Log("Cube exited the collider.");
        }
    }
}
