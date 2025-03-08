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
    public float particleFadeDelayIn = 0.5f; // Delay before particles fade in
    public float particleFadeDelayOut = 0f; // Delay before particles fade out
    public float maxParticleEmissionRate = 100f; // Max emission rate of particles when fully visible
    public float minParticleEmissionRate = 0f; // Min emission rate of particles when fully invisible

    //References
    public Renderer ghostRenderer;
    private Coroutine fadeCoroutine;
    private ParticleSystem ghostParticles;
    private ParticleSystem.EmissionModule particleEmission;
    private GeistBewegung geistBewegung;

    // Network Variables (Synced across all clients)
    private NetworkList<float> fadeAlphas = new NetworkList<float>();
    private NetworkVariable<float> particleEmissionRate = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    public override void OnNetworkSpawn()
    {
        ghostRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        ghostParticles = GetComponentInChildren<ParticleSystem>(); // Find the particle system
        particleEmission = ghostParticles.emission;
        geistBewegung = GetComponent<GeistBewegung>();

        if(geistBewegung == null)
        {
            Debug.Log("GeistBewegung Script not found!");
        }

        //Debug.Log("Entering onNetworkSpawn...");
        if (IsServer)
        {
            // Start fading logic only on the server:

            // Start periodically fading and reappearing on the server
            //ToDo: Only if not stunned or paralized
            if(!geistBewegung.IsStunned() && !geistBewegung.IsParalyzed())
            {
                InitializeFadeList();
                StartFading();
            }
        }
    }

    void InitializeFadeList()
    {
        if (fadeAlphas.Count == 0)
        {
            foreach (Material mat in ghostRenderer.materials)
            {
                fadeAlphas.Add(1f); // Default to fully visible
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        ghostRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        ghostParticles = GetComponentInChildren<ParticleSystem>(); // Find the particle system
        particleEmission = ghostParticles.emission;
        geistBewegung = GetComponent<GeistBewegung>();
    }

    // Update is called once per frame
    void Update()
    {
        // All Clients apply the fadeAlpha/emission rate network value to ensure synchronization
        ApplyFade();
        ApplyParticleFade(particleEmissionRate.Value);
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
            float fadeDuration = Random.Range(minFadeDuration, maxFadeDuration);

            // Fade out ghost
            Debug.Log("Fading ghost...");
            yield return FadeTo(0f, fadeDuration);
            // Delay for particle system fade out
            yield return new WaitForSeconds(particleFadeDelayOut);
            // Fade out particle system
            Debug.Log("Fading particles...");
            yield return FadeParticlesTo(minParticleEmissionRate, fadeDuration);

            //Stay invisible
            yield return new WaitForSeconds(Random.Range(minInvisibleDuration, maxInvisibleDuration));

            // Fade in particle system
            Debug.Log("Particles reapearing...");
            yield return FadeParticlesTo(maxParticleEmissionRate, fadeDuration);
            // Delay for ghost to fade in
            yield return new WaitForSeconds(particleFadeDelayIn);
            // Fade in ghost
            Debug.Log("Ghost reapearing...");
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
        float[] startAlphas = new float[materials.Length];

        // Store initial alpha values
        for (int i = 0; i < materials.Length; i++)
        {
            startAlphas[i] = materials[i].color.a;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlphaFactor = elapsedTime / duration;

            for (int i = 0; i < materials.Length; i++)
            {
                float newAlpha = Mathf.Lerp(startAlphas[i], targetAlpha, newAlphaFactor);
                fadeAlphas[i] = newAlpha;
            }

            yield return null; //continues execution here next frame
        }

        // Ensure final alpha is set correctly
        for (int i = 0; i < materials.Length; i++)
        {
            fadeAlphas[i] = targetAlpha;
        }
    }

    private void ApplyFade()
    {
        if (ghostRenderer == null) return;

        Material[] materials = ghostRenderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            if (i < fadeAlphas.Count)
            {
                Color color = materials[i].color;
                materials[i].color = new Color(color.r, color.g, color.b, fadeAlphas[i]);
            }
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

    #endregion
}
