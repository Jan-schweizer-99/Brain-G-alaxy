using UnityEngine;

public class EnemyActivator : MonoBehaviour
{
    public int startEnemyIndex = 0;  // Der Startindex, der im Inspector eingestellt werden kann
    private int currentEnemyIndex;
    private GameObject[] enemyObjects;
    private Animation currentAnimation;
    private EventSystem eventSystem;
    private float normalizedPosition;
    public ParentLightSwitch parentLightSwitch;

    // Start is called before the first frame update
    void Start()
    {
        enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        currentEnemyIndex = Mathf.Clamp(startEnemyIndex, 0, enemyObjects.Length - 1); // Starte mit dem eingestellten Startindex
        DeactivateAllExceptCurrent();

        eventSystem = EventSystem.Instance; // Annahme: Es gibt eine Klasse EventSystem mit einer Instanz-Methode Instance
        normalizedPosition = eventSystem.LastNormalizedPosition;

        // Holen Sie sich die Animation-Komponente des aktuellen GameObjects
        if (enemyObjects.Length > 0)
        {
            currentAnimation = enemyObjects[currentEnemyIndex].GetComponent<Animation>();

            // Starte die Animation des aktuellen GameObjects
            if (currentAnimation != null)
            {
                currentAnimation.Play();
            }
        }
    }

    void DeactivateAllExceptCurrent()
    {
        // Überprüfe, ob es mehr als ein GameObject gibt
        if (enemyObjects.Length > 1)
        {
            // Iteriere durch alle GameObjects und deaktiviere sie, außer dem aktuellen Index
            for (int i = 0; i < enemyObjects.Length; i++)
            {
                if (i != currentEnemyIndex)
                {
                    enemyObjects[i].SetActive(false);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        normalizedPosition = eventSystem.LastNormalizedPosition;

        if (normalizedPosition < 1)
        {
            // Wenn normalize Position kleiner als 1 ist, spiele die Animation ab
            if (currentAnimation != null && !currentAnimation.isPlaying)
            {
                // Deaktiviere das aktuelle GameObject
                enemyObjects[currentEnemyIndex].SetActive(false);

                // Inkrementiere den Index für das nächste GameObject im Array
                currentEnemyIndex = (currentEnemyIndex + 1) % enemyObjects.Length;

                // Überprüfe, ob es noch weitere GameObjects gibt
                if (currentEnemyIndex < enemyObjects.Length)
                {
                    // Aktiviere das nächste GameObject
                    enemyObjects[currentEnemyIndex].SetActive(true);

                    // Holen Sie sich die Animation-Komponente des nächsten GameObjects
                    currentAnimation = enemyObjects[currentEnemyIndex].GetComponent<Animation>();

                    // Starte die Animation des nächsten GameObjects
                    if (currentAnimation != null)
                    {
                        currentAnimation.Play();
                    }
                }
            }
        }
        else
        {
            // Wenn normalize Position 1 oder höher ist, deaktiviere alle GameObjects
            DeactivateAll();
        }
    }

    void DeactivateAll()
    {
        // Iteriere durch alle GameObjects und deaktiviere sie
        for (int i = 0; i < enemyObjects.Length; i++)
        {
            enemyObjects[i].SetActive(false);
        }
        
        parentLightSwitch.RandomlyDeactivateActiveLights(36);
    }
}
