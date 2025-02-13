// EventSystem.cs

using UnityEngine;

public class EventSystem : MonoBehaviour
{
    private static EventSystem instance;

    public static EventSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("EventSystem").AddComponent<EventSystem>();
            }
            return instance;
        }
    }
    public AudioClip[] audioClips;
    public AudioSource audioSource;
    private float lastNormalizedPosition;
        public float LastNormalizedPosition
    {
        get { return lastNormalizedPosition; }
        set { lastNormalizedPosition = value; }
    }
    public float timerSpeed;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Start()
    {
        lastNormalizedPosition = 1f;
        timerSpeed = 0.0085f;
        audioSource = gameObject.GetComponent<AudioSource>();
        //InvokeSetLastNormalizedPositionForAll();
    }

    // ...
private void Update()
{
    if (lastNormalizedPosition == 1f)
    {
        audioSource.mute = true;
    }
    else
    {
        audioSource.mute = false;
    }

    // Überwache den Timer und aktualisiere ihn
    if (lastNormalizedPosition < 1f)
    {
        if (lastNormalizedPosition > 0.75f)
        {
            PlayAudioWithIndex(0); // Normale Geschwindigkeit
        }
        else if (lastNormalizedPosition > 0.5f)
        {
            PlayAudioWithIndex(1); // Normale Geschwindigkeit
        }
        else if (lastNormalizedPosition > 0.25f)
        {
            PlayAudioWithIndex(2); // Geschwindigkeit auf 150% setzen
        }
        else if (lastNormalizedPosition >= 0f)
        {
            PlayAudioWithIndex(3); // Geschwindigkeit auf 200% setzen
        }

        lastNormalizedPosition += Time.deltaTime * timerSpeed;
        if (lastNormalizedPosition >= 1f)
        {
            lastNormalizedPosition = 1f;
        }
    }

    GameObject[] Dart = GameObject.FindGameObjectsWithTag("Dart");

    // Durchlaufe alle gefundenen GameObjects und führe Aktionen aus
    foreach (GameObject obj in Dart)
    {
        obj.transform.rotation = Quaternion.Euler(obj.transform.rotation.eulerAngles.x, obj.transform.rotation.eulerAngles.y, 360f * lastNormalizedPosition);
    }
}

void PlayAudioWithIndex(int audioIndex)
{
    // Stelle sicher, dass das AudioArray initialisiert wurde und der Index gültig ist
    if (audioClips != null && audioIndex >= 0 && audioIndex < audioClips.Length)
    {
        // Überprüfe, ob eine Audioquelle vorhanden ist
        if (audioSource != null)
        {
            // Setze das gewünschte Audiofile nur, wenn es nicht bereits dasselbe ist
            if (audioSource.clip != audioClips[audioIndex])
            {
                audioSource.clip = audioClips[audioIndex];
                audioSource.Play(); // Spiele die Audioquelle ab oder setze fort, wenn sie bereits läuft
            }
        }
        else
        {
            Debug.LogError("AudioSource nicht gefunden!");
        }
    }
    else
    {
        Debug.LogError("Ungültiger Audio-Index oder Audio-Array nicht initialisiert!");
    }
}




    public void SetLastNormalizedPosition(float normalizedPosition)
    {
        lastNormalizedPosition = normalizedPosition;
        
        ControllerPositionManager[] positionManagers = FindObjectsOfType<ControllerPositionManager>();

        foreach (ControllerPositionManager positionManager in positionManagers)
        {
            Debug.Log("Normalized Position: " + positionManager);
            positionManager.EnablePositionPrinting();
            positionManager.SetYPositionFromNormalizedWithAnimation(lastNormalizedPosition);
            positionManager.DisablePositionPrinting();
        }
    }

    public float GetLastNormalizedPosition()
    {
        return lastNormalizedPosition;
    }
}