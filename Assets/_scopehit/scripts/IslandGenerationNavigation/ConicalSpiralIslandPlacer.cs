using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif




#if UNITY_EDITOR
[CustomEditor(typeof(ConicalSpiralIslandPlacer))]
public class ConicalSpiralIslandPlacerEditor : CustomBaseEditor
{
    protected override void OnEnable()
    {
        SetEditorStyle("IslandSystem");
    }

}
#endif
[ExecuteInEditMode]
public class ConicalSpiralIslandPlacer : MonoBehaviour
{
    // Event das aufgerufen wird, wenn Inseln generiert wurden
    public System.Action OnIslandsGenerated;

    [Header("Manager Referenz")]
    public IslandPrefabManager islandManager;

    [Header("Spirale Einstellungen")]
    public float startRadius = 1f;
    public float heightIncrement = 0.5f;
    public float radiusDecrement = 0.1f;
    public float rotationPerStep = 30f;
    public float spiralMeshYOffset = 0f;

    [Header("Präfab Einstellungen")]
    public GameObject[] islandPrefabs;
    public bool autoLoadPrefabsFromFolder = false;
    public string prefabFolderPath = "Assets/Prefabs/Islands";

    [Header("Zentrum Einstellungen")]
    public GameObject roomOfNumbersPrefab;

    [Header("Partikel Einstellungen")]
    public GameObject particleSystemPrefab;
    public bool showParticleSystem = true;
    public float startParticleSize = 0.5f;
    public float gravityModifier = 0.1f;
    public float particleMultiplier = 1f;
    private GameObject particleSystemInstance;

    // Zur Erkennung von Änderungen
    private int lastArrayLength = 0;
    private float lastStartRadius;
    private float lastHeightIncrement;
    private float lastRadiusDecrement;
    private float lastRotationPerStep;
    private float lastSpiralMeshYOffset;
    private float lastStartParticleSize;
    private float lastGravityModifier;
    private float lastParticleMultiplier;
    private bool lastAutoLoadState;
    private bool lastShowParticleSystem;

    private GameObject spiralMeshObject;

    #if UNITY_EDITOR
    private static bool isCompiling = false;

    [InitializeOnLoadMethod]
    static void RegisterCallback()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += () =>
            {
                var placers = GameObject.FindObjectsOfType<ConicalSpiralIslandPlacer>();
                foreach (var placer in placers)
                {
                    placer.RegenerateAll();
                }
            };
        }
    }

    private static void OnBeforeAssemblyReload()
    {
        isCompiling = true;
    }

    static void OnAfterAssemblyReload()
    {
        isCompiling = false;
        EditorApplication.delayCall += () =>
        {
            var placers = GameObject.FindObjectsOfType<ConicalSpiralIslandPlacer>();
            foreach (var placer in placers)
            {
                placer.RegenerateAll();
            }
        };
    }

    void OnEnable()
    {
        // Initiale Werte speichern
        lastArrayLength = islandPrefabs != null ? islandPrefabs.Length : 0;
        lastStartRadius = startRadius;
        lastHeightIncrement = heightIncrement;
        lastRadiusDecrement = radiusDecrement;
        lastRotationPerStep = rotationPerStep;
        lastSpiralMeshYOffset = spiralMeshYOffset;
        lastStartParticleSize = startParticleSize;
        lastGravityModifier = gravityModifier;
        lastParticleMultiplier = particleMultiplier;
        lastAutoLoadState = autoLoadPrefabsFromFolder;
        lastShowParticleSystem = showParticleSystem;

        // Registriere den Event Listener
        if (islandManager != null)
        {
            islandManager.OnIslandsChanged += OnIslandsChanged;
        }

        // Initial generieren
        RegenerateAll();
    }

    void OnDisable()
    {
        // Aufräumen beim Deaktivieren
        if (!Application.isPlaying)
        {
            ClearAllChildren();
        }

        // Entferne den Event Listener
        if (islandManager != null)
        {
            islandManager.OnIslandsChanged -= OnIslandsChanged;
        }
    }

    private void OnIslandsChanged()
    {
        // Verzögere die Regeneration um einen Frame, damit alle Asset-Operationen abgeschlossen sind
        EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject.activeInHierarchy)
            {
                RegenerateAll();
            }
        };
    }

    void OnValidate()
    {
        // Verhindere Updates während des Kompilierens
        if (isCompiling) return;

        // Verzögere die Ausführung um einen Frame
        EditorApplication.delayCall += () =>
        {
            if (this == null) return; // Falls das Objekt zerstört wurde

            // Prüfe ob sich relevante Werte geändert haben
            if (!Application.isPlaying && 
                (islandPrefabs == null || // Neue Null-Check
                 (islandPrefabs != null && islandPrefabs.Length != lastArrayLength) ||
                 startRadius != lastStartRadius ||
                 heightIncrement != lastHeightIncrement ||
                 radiusDecrement != lastRadiusDecrement ||
                 rotationPerStep != lastRotationPerStep ||
                 spiralMeshYOffset != lastSpiralMeshYOffset ||
                 startParticleSize != lastStartParticleSize ||
                 gravityModifier != lastGravityModifier ||
                 particleMultiplier != lastParticleMultiplier ||
                 showParticleSystem != lastShowParticleSystem ||
                 autoLoadPrefabsFromFolder != lastAutoLoadState))
            {
                RegenerateAll();

                // Neue Werte speichern
                lastArrayLength = islandPrefabs != null ? islandPrefabs.Length : 0;
                lastStartRadius = startRadius;
                lastHeightIncrement = heightIncrement;
                lastRadiusDecrement = radiusDecrement;
                lastRotationPerStep = rotationPerStep;
                lastSpiralMeshYOffset = spiralMeshYOffset;
                lastStartParticleSize = startParticleSize;
                lastGravityModifier = gravityModifier;
                lastParticleMultiplier = particleMultiplier;
                lastShowParticleSystem = showParticleSystem;
                lastAutoLoadState = autoLoadPrefabsFromFolder;
            }
        };
    }

    public void RegenerateAll()
    {
        if (!gameObject.activeInHierarchy) return;

        // Wenn autoLoad aktiviert ist, lade Prefabs aus dem Ordner
        if (autoLoadPrefabsFromFolder)
        {
            LoadPrefabsFromFolder();
        }

        ClearAllChildren();
        GenerateConicalSpiral();
        CreateSpiralMesh();
        UpdateParticleSystem();
    }

    private void LoadPrefabsFromFolder()
    {
        #if UNITY_EDITOR
        if (string.IsNullOrEmpty(prefabFolderPath)) return;

        // Alle Prefab-GUIDs im angegebenen Ordner finden
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        List<GameObject> loadedPrefabs = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                loadedPrefabs.Add(prefab);
            }
        }

        // Prefabs dem Array zuweisen
        islandPrefabs = loadedPrefabs.ToArray();
        #endif
    }

    void ClearAllChildren()
    {
        // Alle Kinder vollständig löschen
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // Falls es ein existierendes Spiral-Mesh gibt, löschen
        if (spiralMeshObject != null)
        {
            DestroyImmediate(spiralMeshObject);
        }

        // Falls es ein existierendes Partikelsystem gibt, löschen
        if (particleSystemInstance != null)
        {
            DestroyImmediate(particleSystemInstance);
        }
    }

    public void GenerateConicalSpiral()
    {
        if (islandPrefabs == null || islandPrefabs.Length == 0)
        {
            Debug.LogWarning("Keine Insel-Präfabs zugewiesen!");
            return;
        }

        float currentRadius = startRadius;
        float currentHeight = 0;
        float highestHeight = 0;
        float lowestHeight = 0;

        // Ersten Durchlauf zur Höhenbestimmung
        for (int i = 0; i < islandPrefabs.Length; i++)
        {
            if (islandPrefabs[i] == null) continue;

            // Höhen aktualisieren
            highestHeight = Mathf.Max(highestHeight, currentHeight);
            lowestHeight = Mathf.Min(lowestHeight, currentHeight);

            // Radius und Höhe für nächste Iteration anpassen
            currentRadius -= radiusDecrement;
            currentHeight += heightIncrement;
        }

        // Mittlere Höhe berechnen
        float centerHeight = (highestHeight + lowestHeight) / 2f;

        // RoomOfNumbers in der Mitte platzieren
        if (roomOfNumbersPrefab != null)
        {
            GameObject centerRoom = Instantiate(roomOfNumbersPrefab, transform);
            centerRoom.transform.localPosition = new Vector3(0, centerHeight, 0);
            centerRoom.name = "RoomOfNumbers_Center";
        }

        // Spirale generieren
        currentRadius = startRadius;
        currentHeight = 0;

        for (int i = 0; i < islandPrefabs.Length; i++)
        {
            if (islandPrefabs[i] == null) continue;

            // Aktuelle Position berechnen
            float currentAngle = i * rotationPerStep * Mathf.Deg2Rad;
            
            float currentX = currentRadius * Mathf.Cos(currentAngle);
            float currentZ = currentRadius * Mathf.Sin(currentAngle);

            // Präfab instanziieren
            GameObject selectedPrefab = islandPrefabs[i];
            GameObject newIsland = Instantiate(selectedPrefab, transform);

            // Position setzen
            newIsland.transform.localPosition = new Vector3(currentX, currentHeight, currentZ);
            
            // Rotation zur Mitte ausrichten
            Vector3 directionToCenter = Vector3.zero - new Vector3(currentX, 0, currentZ);
            newIsland.transform.rotation = Quaternion.LookRotation(directionToCenter);
            
            // Name setzen - verwende den Original-Prefab-Namen wenn autoLoad aktiv ist
            if (autoLoadPrefabsFromFolder)
            {
                newIsland.name = selectedPrefab.name;
            }
            else
            {
                newIsland.name = $"Island_{i + 1}_{selectedPrefab.name}";
            }

            // Radius und Höhe anpassen
            currentRadius -= radiusDecrement;
            currentHeight += heightIncrement;
        }

        // Event auslösen
        OnIslandsGenerated?.Invoke();
    }

    private void CreateSpiralMesh()
    {
        spiralMeshObject = new GameObject("SpiralMesh");
        spiralMeshObject.transform.parent = transform;
        spiralMeshObject.transform.localPosition = new Vector3(0, spiralMeshYOffset, 0);

        MeshFilter meshFilter = spiralMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = spiralMeshObject.AddComponent<MeshRenderer>();

        // Mesh Renderer deaktivieren
        meshRenderer.enabled = false;

        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        Mesh spiralMesh = new Mesh();

        // Spiralenpunkte berechnen
        Vector3[] vertices = new Vector3[islandPrefabs.Length];
        int[] indices = new int[(islandPrefabs.Length - 1) * 2];

        float currentRadius = startRadius;
        float currentHeight = 0;

        for (int i = 0; i < islandPrefabs.Length; i++)
        {
            float angle = i * rotationPerStep * Mathf.Deg2Rad;
            vertices[i] = new Vector3(currentRadius * Mathf.Cos(angle), currentHeight, currentRadius * Mathf.Sin(angle));

            if (i < islandPrefabs.Length - 1)
            {
                indices[i * 2] = i;
                indices[i * 2 + 1] = i + 1;
            }

            currentRadius -= radiusDecrement;
            currentHeight += heightIncrement;
        }

        spiralMesh.vertices = vertices;
        spiralMesh.SetIndices(indices, MeshTopology.Lines, 0);
        meshFilter.mesh = spiralMesh;
    }

    private void UpdateParticleSystem()
    {
        // Zuerst altes Partikelsystem entfernen falls vorhanden
        if (particleSystemInstance != null)
        {
            DestroyImmediate(particleSystemInstance);
        }

        // Wenn Partikelsystem deaktiviert ist oder kein Prefab vorhanden, hier abbrechen
        if (!showParticleSystem || particleSystemPrefab == null) return;

        // Partikelsystem instanziieren
        particleSystemInstance = Instantiate(particleSystemPrefab, transform);
        particleSystemInstance.name = "SpiralParticleSystem";
        
        // Partikelsystem-Komponente abrufen
        var particleSystem = particleSystemInstance.GetComponent<ParticleSystem>();
        if (particleSystem == null) return;

        // Hauptmodul konfigurieren
        var mainModule = particleSystem.main;
        mainModule.startSize = startParticleSize;
        mainModule.gravityModifier = gravityModifier;

        // Emission anpassen
        var emissionModule = particleSystem.emission;
        var rate = emissionModule.rateOverTime;
        rate.constant *= particleMultiplier;
        emissionModule.rateOverTime = rate;

        // Shape Modul konfigurieren
        var shapeModule = particleSystem.shape;
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Mesh;
        shapeModule.mesh = spiralMeshObject.GetComponent<MeshFilter>().sharedMesh;
        
        // Position des Partikelsystems anpassen
        particleSystemInstance.transform.localPosition = new Vector3(0, spiralMeshYOffset, 0);
    }
    #endif
}