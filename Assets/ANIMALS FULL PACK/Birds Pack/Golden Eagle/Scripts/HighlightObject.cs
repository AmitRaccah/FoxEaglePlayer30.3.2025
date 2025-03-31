using UnityEngine;

/// <summary>
/// Controls the emission (glow) of a Renderer component for highlighting objects during vision mode.
/// Supports a pulsating glow effect while vision is active and can hide the object when vision is off.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class HighlightEmissionController : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Color used for highlighting (e.g., Yellow, Red, etc.).")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [Tooltip("Base emission intensity.")]
    [SerializeField] private float baseIntensity = 1f;

    [Header("Pulsation Settings")]
    [Tooltip("Enable pulsating glow effect.")]
    [SerializeField] private bool enablePulsation = true;
    [Tooltip("Speed of the pulsation effect.")]
    [SerializeField] private float pulseSpeed = 2f;
    [Tooltip("Amplitude of pulsation.")]
    [SerializeField] private float pulseAmplitude = 0.5f;

    [Header("Visibility Settings")]
    [Tooltip("If true, object is hidden when vision mode is off.")]
    [SerializeField] private bool hideWhenNotHighlighted = false;

    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private Color originalEmission;
    private bool isHighlighted = false;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("Renderer not found on " + gameObject.name);
            enabled = false;
            return;
        }
        propBlock = new MaterialPropertyBlock();
        rend.GetPropertyBlock(propBlock);
        originalEmission = propBlock.GetColor("_EmissionColor");
    }

    private void OnEnable()
    {
        UnifiedVisionController.OnVisionToggle += HandleVisionToggle;
    }

    private void OnDisable()
    {
        UnifiedVisionController.OnVisionToggle -= HandleVisionToggle;
    }

    private void Update()
    {
        if (isHighlighted && enablePulsation)
        {
            float pulsatingIntensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            UpdateEmission(pulsatingIntensity);
        }
    }

    public void EnableHighlight()
    {
        isHighlighted = true;
        if (hideWhenNotHighlighted)
            rend.enabled = true;
        UpdateEmission(baseIntensity);
    }

    public void DisableHighlight()
    {
        isHighlighted = false;
        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", originalEmission);
        rend.SetPropertyBlock(propBlock);
        rend.material.DisableKeyword("_EMISSION");
        if (hideWhenNotHighlighted)
            rend.enabled = false;
    }

    private void UpdateEmission(float intensity)
    {
        rend.GetPropertyBlock(propBlock);
        Color newEmission = highlightColor * intensity;
        propBlock.SetColor("_EmissionColor", newEmission);
        rend.SetPropertyBlock(propBlock);
        rend.material.EnableKeyword("_EMISSION");
    }

    private void HandleVisionToggle(bool isActive)
    {
        if (isActive)
        {
            if (hideWhenNotHighlighted)
                rend.enabled = true;
            EnableHighlight();
        }
        else
        {
            DisableHighlight();
            if (hideWhenNotHighlighted)
                rend.enabled = false;
        }
    }
}
