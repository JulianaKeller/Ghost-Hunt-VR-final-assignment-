using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GhostFadeEffects : NetworkBehaviour
{
    [Header("Fading Settings")]
    public float minFadeDuration = 5f;
    public float maxFadeDuration = 15f;
    public float minVisibleDuration = 10f;
    public float maxVisibleDuration = 45f;
    public float minInvisibleDuration = 10f;
    public float maxInvisibleDuration = 30f;

    [Header("Particle Fading Settings")]
    public float particleFadeDelayIn = 0.5f;
    public float particleFadeDelayOut = 0f;
    public float maxParticleEmissionRate = 100f;
    public float minParticleEmissionRate = 0f;

    //References
    public Renderer ghostRenderer;
    private Coroutine fadeCoroutine;
    private ParticleSystem ghostParticles;
    private ParticleSystem.EmissionModule particleEmission;
    private GeistBewegung geistBewegung;

    // Network Variables (Synced across all clients)
    private NetworkVariable<float> fadeAlpha = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> particleEmissionRate = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        minVisibleDuration = NetworkVariableManager.Instance.GetDifficultyProperties().GhostMinVisibilityDuration;

        ghostRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        ghostParticles = GetComponentInChildren<ParticleSystem>();
        particleEmission = ghostParticles.emission;
        geistBewegung = GetComponent<GeistBewegung>();

        if(geistBewegung == null)
        {
            Debug.Log("GeistBewegung Script not found!");
        }

        //Debug.Log("Entering onNetworkSpawn...");
        if (IsServer)
        {
            StartFading();
        }
    }

    void Start()
    {
        ghostRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        ghostParticles = GetComponentInChildren<ParticleSystem>();
        particleEmission = ghostParticles.emission;
        geistBewegung = GetComponent<GeistBewegung>();
    }

    void Update()
    {
        // If the ghost is not stunned or paralyzed, start or restart fading:
        if (!geistBewegung.IsStunned() && !geistBewegung.IsParalyzed())
        {
            if (fadeCoroutine == null) // Ensure the routine is not already running
            {
                fadeCoroutine = StartCoroutine(FadeInOutRoutine());
            }
            // All Clients apply the fadeAlpha/emission rate network value to ensure synchronization
            ApplyFade();
            ApplyParticleFade(particleEmissionRate.Value);
        }
        else
        {
            // If the ghost is stunned or paralyzed, stop the fading routine
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null; // Reset the coroutine reference
            }
            ResetAlphaToDefault();
            SetParticleEmission(0f);
        }
    }

    #region Fading methods

    void StartFading()
    {
        //Debug.Log("Starting StartFading...");
        if (fadeCoroutine != null) //Ensure only one fading routine is running at a time
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeInOutRoutine());
    }

    private IEnumerator FadeInOutRoutine()
    {
        //Debug.Log("Starting FadeInOutRoutine...");
        while (true)
        {
                minVisibleDuration = NetworkVariableManager.Instance.GetDifficultyProperties().GhostMinVisibilityDuration;

                float fadeDuration = Random.Range(minFadeDuration, maxFadeDuration);

                // Fade out ghost
                //Debug.Log("Fading ghost...");
                yield return FadeTo(0f, fadeDuration);
                // Delay for particle system fade out
                yield return new WaitForSeconds(particleFadeDelayOut);
                // Fade out particle system
                //Debug.Log("Fading particles...");
                yield return FadeParticlesTo(minParticleEmissionRate, fadeDuration);

                //Stay invisible
                yield return new WaitForSeconds(Random.Range(minInvisibleDuration, maxInvisibleDuration));

                // Fade in particle system
                //Debug.Log("Particles reapearing...");
                yield return FadeParticlesTo(maxParticleEmissionRate, fadeDuration);
                // Delay for ghost to fade in
                yield return new WaitForSeconds(particleFadeDelayIn);
                // Fade in ghost
                //Debug.Log("Ghost reapearing...");
                yield return FadeTo(1f, fadeDuration);

                //Stay visible
                yield return new WaitForSeconds(Random.Range(minVisibleDuration, maxVisibleDuration));
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        //Debug.Log("Starting FadeTo...");
        if (ghostRenderer == null)
        {
            Debug.Log("No renderer assigned!!!");
        }
        Material[] materials = ghostRenderer.materials;
        //Debug.Log("Materials: " + materials.Length);

        float elapsedTime = 0f;
        float startAlpha = materials[0].color.a;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlphaFactor = elapsedTime / duration;

            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, newAlphaFactor);
            fadeAlpha.Value = newAlpha;

            yield return null; //continues execution here next frame
        }

        // Ensure final alpha is set correctly
        fadeAlpha.Value = targetAlpha;
    }

    private void ApplyFade()
    {
        if (ghostRenderer == null) return;

        Material[] materials = ghostRenderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Color color = materials[i].color;
            materials[i].color = new Color(color.r, color.g, color.b, fadeAlpha.Value);
        }
    }

    private IEnumerator FadeParticlesTo(float targetRate, float duration)
    {
        //Debug.Log("Starting FadeParticlesTo...");
        float startRate = particleEmission.rateOverTime.constant;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newRate = Mathf.Lerp(startRate, targetRate, elapsedTime / duration);
            //Update emission rate network variable
            particleEmissionRate.Value = newRate;
            //Debug.Log("Server: Setting particle emission rate to " + particleEmissionRate.Value);

            yield return null; // Continues execution here next frame
        }

        // Ensure final emission rate is set correctly
        particleEmissionRate.Value = targetRate;
    }

    private void ApplyParticleFade(float targetRate)
    {
        // Apply the synchronized emission rate to the particle system
        particleEmission.rateOverTime = targetRate;
        //Debug.Log("Client: Applying particle emission rate to " + particleEmissionRate.Value);
    }

    private void ResetAlphaToDefault()
    {
        if (ghostRenderer != null)
        {
            Material[] materials = ghostRenderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                Color color = materials[i].color;
                materials[i].color = new Color(color.r, color.g, color.b, 1f);  // Set alpha to 1 (fully visible)
            }
            fadeAlpha.Value = 1f;
        }
    }

    private void SetParticleEmission(float rate)
    {
        if (ghostParticles != null)
        {
            var emission = ghostParticles.emission;
            emission.rateOverTime = rate; 
            particleEmissionRate.Value = rate;
        }
    }

    #endregion
}
