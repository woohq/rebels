using UnityEngine;
using System.Collections;

public class WarpSpeedTransition : MonoBehaviour
{
    [Header("Charge-Up Effect")]
    [SerializeField] private float chargeUpDuration = 2.5f;
    [SerializeField] private float fieldOfViewDecrease = 20.0f;
    [SerializeField] private ParticleSystem chargeUpParticles;
    [SerializeField] private AudioClip chargeUpSound;
    [SerializeField] private float chargeUpIntensityMultiplier = 1.5f;

    [Header("Warp Speed Effect")]
    [SerializeField] private float warpDuration = 4.0f;
    [SerializeField] private float initialFovJump = 40.0f;
    [SerializeField] private float additionalFovIncrease = 15.0f;
    [SerializeField] private float maxSpeedMultiplier = 5.0f;
    [SerializeField] private float holdDuration = 1.0f;

    [Header("Star Field Effect")]
    [SerializeField] private Material starfieldMaterial;
    [SerializeField] private float normalScrollSpeed = 0.1f;
    [SerializeField] private float warpStreakLength = 10f;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem warpParticles;
    [SerializeField] private int particleMultiplier = 5;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PathController pathController;

    [Header("Audio")]
    [SerializeField] private AudioClip warpSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Flash Effect")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private float maxFlashIntensity = 0.8f;

    // Events
    public delegate void WarpEvent();
    public event WarpEvent OnWarpMidpoint;
    public event WarpEvent OnWarpComplete;
    public event WarpEvent OnChargeUpComplete;

    // Private fields
    private float originalFOV;
    private float originalSpeed;
    private float originalScrollSpeed;
    private bool isWarping = false;
    private Texture2D flashTexture;
    private float currentFlashAlpha = 0f;

    private void Awake()
    {
        InitializeComponents();

        // Create flash texture
        flashTexture = new Texture2D(1, 1);
        flashTexture.SetPixel(0, 0, Color.white);
        flashTexture.Apply();
    }

    private void Start()
    {
        StoreOriginalValues();

        // Disable particles initially
        SetParticlesActive(warpParticles, false);
        SetParticlesActive(chargeUpParticles, false);

        // Initialize starfield for normal flight
        if (starfieldMaterial != null)
        {
            // Store original scroll speed if it exists in the material
            if (starfieldMaterial.HasProperty("_ScrollSpeed"))
            {
                originalScrollSpeed = starfieldMaterial.GetFloat("_ScrollSpeed");
            }
            else
            {
                // Set default scroll speed if property doesn't exist yet
                originalScrollSpeed = normalScrollSpeed;
            }

            // Initialize starfield properties
            starfieldMaterial.SetFloat("_ScrollSpeed", originalScrollSpeed);

            // Make sure warp effect is off initially
            if (starfieldMaterial.HasProperty("_WarpStrength"))
            {
                starfieldMaterial.SetFloat("_WarpStrength", 0f);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up texture when destroyed
        if (flashTexture != null)
        {
            Destroy(flashTexture);
        }
    }

    private void InitializeComponents()
    {
        // Find references if not assigned in inspector
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (pathController == null)
            pathController = GetComponent<PathController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void StoreOriginalValues()
    {
        if (playerCamera != null)
            originalFOV = playerCamera.fieldOfView;
    }

    public void StartWarpEffect()
    {
        if (!isWarping)
        {
            StartCoroutine(WarpSequence());
        }
    }

    public float GetWarpDuration()
    {
        // Total duration including charge-up and warp
        return chargeUpDuration + warpDuration + holdDuration;
    }

    private IEnumerator WarpSequence()
    {
        isWarping = true;

        // First, run the charge-up effect
        yield return ChargeUpEffect();

        // Invoke charge-up complete event
        OnChargeUpComplete?.Invoke();

        // Then, run the warp effect
        yield return WarpEffectCoroutine();

        isWarping = false;
    }

    private IEnumerator ChargeUpEffect()
    {
        // Play charge-up sound
        PlaySound(chargeUpSound);

        // Activate charge-up particles
        SetParticlesActive(chargeUpParticles, true);

        // Gradually intensify the charge-up effect
        float elapsedTime = 0f;
        while (elapsedTime < chargeUpDuration)
        {
            float t = elapsedTime / chargeUpDuration;

            // Apply decreasing FOV
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFOV - (fieldOfViewDecrease * t);
            }

            // Increase particle intensity
            if (chargeUpParticles != null)
            {
                var emission = chargeUpParticles.emission;
                var rate = emission.rateOverTime;
                emission.rateOverTime = rate.constant * (1f + t * chargeUpIntensityMultiplier);
            }

            // Slow down star scrolling during charge up
            if (starfieldMaterial != null && starfieldMaterial.HasProperty("_ScrollSpeed"))
            {
                starfieldMaterial.SetFloat("_ScrollSpeed", originalScrollSpeed * (1f - 0.8f * t));
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hold at maximum charge for a moment
        yield return new WaitForSeconds(0.2f);

        // Stop charge-up particles
        SetParticlesActive(chargeUpParticles, false);
    }

    private IEnumerator WarpEffectCoroutine()
    {
        // Play warp sound
        PlaySound(warpSound);

        // Activate warp particles
        if (warpParticles != null)
        {
            var emission = warpParticles.emission;
            var rate = emission.rateOverTime;
            emission.rateOverTime = rate.constant * particleMultiplier;
            warpParticles.gameObject.SetActive(true);
        }

        // Instant jump to higher FOV
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV + initialFovJump;
        }

        // First half of warp - accelerate
        float firstHalfDuration = warpDuration * 0.4f;
        float elapsedTime = 0f;

        while (elapsedTime < firstHalfDuration)
        {
            float t = elapsedTime / firstHalfDuration;

            // Apply additional FOV increase on top of the initial jump
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFOV + initialFovJump + (additionalFovIncrease * t);
            }

            // Update starfield effect - gradually increase warp effect
            if (starfieldMaterial != null)
            {
                // Apply warp strength if property exists
                if (starfieldMaterial.HasProperty("_WarpStrength"))
                {
                    starfieldMaterial.SetFloat("_WarpStrength", t);
                }

                // Increase scroll speed for stars streaking by
                if (starfieldMaterial.HasProperty("_ScrollSpeed"))
                {
                    starfieldMaterial.SetFloat("_ScrollSpeed", normalScrollSpeed * (1f + t * 5f));
                }

                // Apply streak length if property exists
                if (starfieldMaterial.HasProperty("_StreakLength"))
                {
                    starfieldMaterial.SetFloat("_StreakLength", warpStreakLength * t);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hold at peak for a moment
        // Start flash effect before teleportation
        StartCoroutine(FlashEffect(true));

        yield return new WaitForSeconds(holdDuration);


        // Wait until flash is at its peak
        yield return new WaitForSeconds(flashDuration * 0.5f);

        // Signal the midpoint (teleport) when the screen is at maximum brightness
        OnWarpMidpoint?.Invoke();

        // Wait a brief moment for the level to regenerate
        yield return new WaitForSeconds(0.2f);

        // Start fading out the flash
        StartCoroutine(FlashEffect(false));

        // Second half of warp - decelerate
        float secondHalfDuration = warpDuration * 0.6f;
        elapsedTime = 0f;

        while (elapsedTime < secondHalfDuration)
        {
            float t = 1f - (elapsedTime / secondHalfDuration); // Inverse t - starts at 1, goes to 0

            // Apply decreasing FOV
            if (playerCamera != null)
            {
                float maxFOV = originalFOV + initialFovJump + additionalFovIncrease;
                playerCamera.fieldOfView = originalFOV + initialFovJump + (additionalFovIncrease * t);
            }

            // Update starfield effect - gradually decrease warp effect
            if (starfieldMaterial != null)
            {
                // Decrease warp strength if property exists
                if (starfieldMaterial.HasProperty("_WarpStrength"))
                {
                    starfieldMaterial.SetFloat("_WarpStrength", t);
                }

                // Decrease scroll speed
                if (starfieldMaterial.HasProperty("_ScrollSpeed"))
                {
                    starfieldMaterial.SetFloat("_ScrollSpeed", normalScrollSpeed * (1f + t * 5f));
                }

                // Decrease streak length if property exists
                if (starfieldMaterial.HasProperty("_StreakLength"))
                {
                    starfieldMaterial.SetFloat("_StreakLength", warpStreakLength * t);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset to original values
        ResetToOriginalValues();

        // Deactivate warp particles
        SetParticlesActive(warpParticles, false);

        // Signal the end of the warp
        OnWarpComplete?.Invoke();
    }

    private IEnumerator FlashEffect(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : maxFlashIntensity;
        float endAlpha = fadeIn ? maxFlashIntensity : 0f;
        float duration = fadeIn ? flashDuration * 0.4f : flashDuration * 0.6f; // Faster fade in, slower fade out

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Calculate current alpha (eased for smoother transition)
            currentFlashAlpha = Mathf.Lerp(startAlpha, endAlpha, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we reach the target alpha
        currentFlashAlpha = endAlpha;
    }

    private void ResetToOriginalValues()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SetParticlesActive(ParticleSystem particles, bool active)
    {
        if (particles != null)
        {
            particles.gameObject.SetActive(active);
        }
    }

    // Draw the screen flash effect using OnGUI
    private void OnGUI()
    {
        if (currentFlashAlpha <= 0) return;

        // Create the color with current alpha
        Color color = flashColor;
        color.a = currentFlashAlpha;

        // Draw the flash overlay
        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), flashTexture);
    }
}