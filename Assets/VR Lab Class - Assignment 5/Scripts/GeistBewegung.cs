using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class GeistBewegung : NetworkBehaviour
{
    [Header("Animation and Movement Settings")]
    public float animationSpeed = 1.0f;
    public float movementSpeedMultiplier = 1.0f; // Bewegungsgeschwindigkeit

    [Header("Rotation Settings")]
    public float minRotationAngleBoundary = 90f; // Maximale Rotationswinkel
    public float maxRotationAngleBoundary = 270f;
    public float maxRotationAngleRandomRotation = 90f; // Maximale Rotationswinkel
    public float rotationChanceIncreaseRate = 0.1f; // Erhöhungsrate der Rotationswahrscheinlichkeit

    [Header("Fading Settings")]
    public float minFadeDuration = 5f;
    public float maxFadeDuration = 15f;
    public float minVisibleDuration = 10f;
    public float maxVisibleDuration = 45f;
    public float minInvisibleDuration = 10f;
    public float maxInvisibleDuration = 30f;

    [Header("Particle Fading Settings")]
    public float particleFadeDelayIn = 0.5f; // Delay before particles fade in
    public float particleFadeDelayOut = 0.5f; // Delay before particles fade out
    public float maxParticleEmissionRate = 100f; // Max emission rate of particles when fully visible
    public float minParticleEmissionRate = 0f; // Min emission rate of particles when fully invisible

    [Header("References")]
    public LayerMask Boundary; // Layer für Begrenzungsobjekte
    public LayerMask Ghosts; // Layer für Begrenzungsobjekte

    //Rotation values
    private float timeSinceLastTurn = 0f;
    private float rotationChance = 0f;

    //References
    private Animator animator;
    private Renderer ghostRenderer;
    private Coroutine fadeCoroutine;
    private ParticleSystem ghostParticles;
    private ParticleSystem.EmissionModule particleEmission;

    // Network Variables (Synced across all clients)
    private NetworkVariable<bool> isVisible = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isParalyzed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> walkingV1Chance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> idleChance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkList<float> fadeAlphas = new NetworkList<float>();
    private NetworkVariable<float> particleEmissionRate = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        fadeAlphas = new NetworkList<float>(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        if (IsServer)
        {
            // Start ghost movement and fading logic only on the server:

            //Randomly change walking animation
            InvokeRepeating(nameof(UpdateWalkingAnimation), 2f, 2f); // Alle 2 Sekunden checken
            // Start periodically fading and reappearing on the server
            StartFading();
        }
    }

    void Start()
    {
        //Get the components
        animator = GetComponent<Animator>();
        ghostRenderer = GetComponentInChildren<Renderer>();
        ghostParticles = GetComponentInChildren<ParticleSystem>(); // Find the particle system
        particleEmission = ghostParticles.emission;

    }

    void Update()
    {
        if (IsServer) // Only the server should control movement
        {
            if (!isParalyzed.Value && !isStunned.Value && !animator.GetCurrentAnimatorStateInfo(0).IsName("Drunk Idle"))
            {
                MoveForward();
                IncreaseRotationChance();
                TryRandomRotation();
            }
            else
            {
                Debug.Log("Paralyzed or stunned or idle.");
            }
        } 

        animator.speed = animationSpeed;

        // Al Clients apply the network variable values for the animator variables
        animator.SetBool("isStunned", isStunned.Value);
        animator.SetBool("isParalyzed", isParalyzed.Value);
        animator.SetFloat("walkingV1Chance", walkingV1Chance.Value);
        animator.SetFloat("IdleChance", idleChance.Value);

        // All Clients apply the fadeAlpha/emission rate network value to ensure synchronization
        ApplyFade();
        ApplyParticleFade(particleEmissionRate.Value);
    }

    void UpdateWalkingAnimation()
    {
        if (!IsServer) return;

        walkingV1Chance.Value = Random.Range(0.0f, 1.0f);
        idleChance.Value = Random.Range(0.0f, 1.0f);
    }

    void MoveForward()
    {
        transform.Translate(Vector3.forward * movementSpeedMultiplier * Time.deltaTime);
    }

    #region Fading methods

    void StartFading()
    {
        if (fadeCoroutine != null) //Ensure only one fading routine is running at a time
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeInOutRoutine());
    }

    private IEnumerator FadeInOutRoutine()
    {
        while (true)
        {
            float fadeDuration = Random.Range(minFadeDuration, maxFadeDuration);

            // Fade out ghost
            yield return FadeTo(0f, fadeDuration);
            // Delay for particle system fade out
            yield return new WaitForSeconds(particleFadeDelayOut);
            // Fade out particle system
            yield return FadeParticlesTo(minParticleEmissionRate, fadeDuration);

            //Stay invisible
            yield return new WaitForSeconds(Random.Range(minInvisibleDuration, maxInvisibleDuration));

            // Fade in particle system
            yield return FadeParticlesTo(maxParticleEmissionRate, fadeDuration);
            // Delay for ghost to fade in
            yield return new WaitForSeconds(particleFadeDelayIn);
            // Fade in ghost
            yield return FadeTo(1f, fadeDuration);

            //Stay visible
            yield return new WaitForSeconds(Random.Range(minVisibleDuration, maxVisibleDuration));
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        Material[] materials = ghostRenderer.materials;

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
        float startRate = particleEmission.rateOverTime.constant;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newRate = Mathf.Lerp(startRate, targetRate, elapsedTime / duration);
            //Update emission rate network variable
            particleEmissionRate.Value = newRate;

            yield return null; // Continues execution here next frame
        }

        // Ensure final emission rate is set correctly
        particleEmissionRate.Value = targetRate;
    }

    private void ApplyParticleFade(float targetRate)
    {
        // Apply the synchronized emission rate to the particle system
        particleEmission.rateOverTime = targetRate;
    }

    #endregion

    #region Rotation methods

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (((1 << other.gameObject.layer) & Boundary) != 0 || ((1 << other.gameObject.layer) & Ghosts) != 0)
        {
            RotateRandomly(minRotationAngleBoundary, maxRotationAngleBoundary);
            ResetRotationChance();
        }
    }

    void IncreaseRotationChance()
    {
        rotationChance += rotationChanceIncreaseRate * Time.deltaTime;
    }

    void TryRandomRotation()
    {
        if (Random.value < rotationChance)
        {
            RotateRandomly(0f, maxRotationAngleRandomRotation);
            ResetRotationChance();
        }
    }

    void ResetRotationChance()
    {
        rotationChance = 0f;
    }

    void RotateRandomly(float minAngle, float maxAngle)
    {
        float randomAngle = Random.Range(minAngle, maxAngle);
        transform.Rotate(Vector3.up, randomAngle);
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server)]
    public void StunLaserServerRpc()
    {
        isStunned.Value = true;
        animator.SetBool("isStunned", true);
    }

    [Rpc(SendTo.Server)]
    public void UnstunLaserServerRpc()
    {
        isStunned.Value = false;
        animator.SetBool("isStunned", false);
    }

    [Rpc(SendTo.Server)]
    public void ParalyzeFlashServerRpc(float duration)
    {
        StartCoroutine(Paralyze(duration));
    }

    private IEnumerator Paralyze(float duration)
    {
        isParalyzed.Value = true;
        yield return new WaitForSeconds(duration);
        isParalyzed.Value = false;
    }

    #endregion

}
