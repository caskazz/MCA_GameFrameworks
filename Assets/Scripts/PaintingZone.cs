using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class PaintingZone : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The 'Paint' action from your Input Actions asset (bound to Q).")]
    [SerializeField] private InputActionReference paintAction;

    [Header("Painting")]
    [SerializeField] private Renderer paintingRenderer;
    [Tooltip("Material the painting starts with. If empty, the renderer's current material is used.")]
    [SerializeField] private Material unpaintedMaterial;
    [Tooltip("Possible end materials. One is picked at random each time the painting is started. They should use the same shader as the unpainted one.")]
    [SerializeField] private Material[] paintedMaterials;

    [Header("Settings")]
    [Tooltip("Seconds of holding the paint key needed to finish the painting.")]
    [SerializeField] private float paintDuration = 6f;
    [Tooltip("Total energy restored by finishing the painting.")]
    [SerializeField] private int energyReward = 5;
    [Tooltip("Seconds the finished painting stays visible before it resets and can be painted again.")]
    [SerializeField] private float finishedDisplayTime = 3f;
    [Tooltip("{key} is replaced with the key bound to the Paint action.")]
    [SerializeField] private string promptText = "Paint with {key}";

    private GameUI gameUI;
    private EnergySystem playerEnergy;
    private Material blendMaterial;
    private Material currentEndMaterial;
    private float progress;
    private int energyGiven;
    private bool completed;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (paintingRenderer == null)
            paintingRenderer = GetComponentInChildren<Renderer>();

        if (unpaintedMaterial == null && paintingRenderer != null)
            unpaintedMaterial = paintingRenderer.sharedMaterial;

        if (paintedMaterials == null || paintedMaterials.Length == 0)
            Debug.LogWarning("PaintingZone: no painted materials assigned on " + name + ", so the painting won't change.", this);

        ResetPainting();
    }

    void OnEnable()
    {
        // The Player Input component normally enables the action already; this is a safety net.
        if (paintAction != null && !paintAction.action.enabled)
            paintAction.action.Enable();
    }

    void Start()
    {
        gameUI = FindAnyObjectByType<GameUI>();

        if (paintAction == null)
        {
            Debug.LogWarning("PaintingZone: no Paint action assigned on " + name);
            return;
        }

        string key = paintAction.action.GetBindingDisplayString();
        promptText = promptText.Replace("{key}", key);
    }

    void OnDestroy()
    {
        if (blendMaterial != null)
            Destroy(blendMaterial);
    }

    void Update()
    {
        if (playerEnergy == null || completed) return;
        if (paintAction == null || !paintAction.action.IsPressed()) return;

        progress = Mathf.Clamp01(progress + Time.deltaTime / paintDuration);

        // Give energy gradually, whole points at a time.
        int shouldHaveGiven = Mathf.FloorToInt(progress * energyReward + 0.0001f);
        int toGive = shouldHaveGiven - energyGiven;
        if (toGive > 0)
        {
            playerEnergy.Recover(toGive);
            energyGiven += toGive;
        }

        ApplyProgressToMaterial();

        if (progress >= 1f)
            CompletePainting();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerEnergy = other.GetComponentInParent<EnergySystem>();
        if (playerEnergy != null && !completed)
            gameUI?.ShowPrompt(promptText);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerEnergy = null;
        gameUI?.HidePrompt();
    }

    void CompletePainting()
    {
        completed = true;
        gameUI?.HidePrompt();

        // Swap to the exact final material so textures etc. are fully correct.
        if (paintingRenderer != null && currentEndMaterial != null)
            paintingRenderer.sharedMaterial = currentEndMaterial;

        // Leave the finished painting visible for a moment, then start over.
        if (finishedDisplayTime > 0f)
            Invoke(nameof(ResetPainting), finishedDisplayTime);
        else
            ResetPainting();
    }

    void ResetPainting()
    {
        progress = 0f;
        energyGiven = 0;
        completed = false;

        // Pick the material this round will end with (a different one than last time when possible).
        currentEndMaterial = PickEndMaterial();

        if (paintingRenderer != null && unpaintedMaterial != null)
        {
            Material oldBlend = blendMaterial;

            // .material creates a per-object copy we can blend without touching the asset.
            paintingRenderer.sharedMaterial = unpaintedMaterial;
            blendMaterial = paintingRenderer.material;

            if (oldBlend != null)
                Destroy(oldBlend);
        }

        // If the player is standing in the zone when it resets, show the prompt again.
        if (playerEnergy != null)
            gameUI?.ShowPrompt(promptText);
    }

    Material PickEndMaterial()
    {
        if (paintedMaterials == null || paintedMaterials.Length == 0) return null;
        if (paintedMaterials.Length == 1) return paintedMaterials[0];

        Material pick = null;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            pick = paintedMaterials[Random.Range(0, paintedMaterials.Length)];
            if (pick != null && pick != currentEndMaterial) break;
        }

        return pick;
    }

    void ApplyProgressToMaterial()
    {
        if (blendMaterial == null || unpaintedMaterial == null || currentEndMaterial == null) return;

        // Blends colors and numeric properties (textures are not blended).
        blendMaterial.Lerp(unpaintedMaterial, currentEndMaterial, progress);
    }
}