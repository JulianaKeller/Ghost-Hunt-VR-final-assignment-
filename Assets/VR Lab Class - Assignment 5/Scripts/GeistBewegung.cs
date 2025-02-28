using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GeistBewegung : NetworkBehaviour
{
    public float animationSpeed = 1.0f;
    public float movementSpeedMultiplier = 1.0f; // Bewegungsgeschwindigkeit
    public float minRotationAngleBoundary = 90f; // Maximale Rotationswinkel
    public float maxRotationAngleBoundary = 270f;
    public float maxRotationAngleRandomRotation = 90f; // Maximale Rotationswinkel
    public float rotationChanceIncreaseRate = 0.1f; // Erhöhungsrate der Rotationswahrscheinlichkeit
    public LayerMask Boundary; // Layer für Begrenzungsobjekte
    public LayerMask Ghosts; // Layer für Begrenzungsobjekte

    private float timeSinceLastTurn = 0f;
    private float rotationChance = 0f;
    private Animator animator;

    private bool isParalyzed = false;
    private bool isStunned = false;

    private float WalkingV1Chance = 0;
    private float IdleChance = 0;

    void Start()
    {
        animator = GetComponent<Animator>();

        //Randomly change walking animation
        InvokeRepeating(nameof(UpdateWalkingAnimation), 2f, 2f); // Alle 2 Sekunden checken
    }

    void Update()
    {
        animator.speed = animationSpeed;

        if (!isParalyzed && !isStunned && !animator.GetCurrentAnimatorStateInfo(0).IsName("Drunk Idle"))
        {
            MoveForward();
            IncreaseRotationChance();
            TryRandomRotation();
        }
        else{
            Debug.Log("Paralyzed or stunned or idle.");
        }
    }

    void UpdateWalkingAnimation()
    {
        WalkingV1Chance = Random.Range(0.0f, 1.0f); // Gibt einen Wert zwischen 0 und 1 zurück
        animator.SetFloat("walkingV1Chance", WalkingV1Chance);
        IdleChance = Random.Range(0.0f, 1.0f);
        animator.SetFloat("IdleChance", IdleChance);
    }

    void MoveForward()
    {
        transform.Translate(Vector3.forward * movementSpeedMultiplier * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
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

    [Rpc(SendTo.Server)]
    public void StunLaserServerRpc()
    {
        isStunned = true;
        animator.SetBool("isStunned", true);
    }

    [Rpc(SendTo.Server)]
    public void UnstunLaserServerRpc()
    {
        isStunned = false;
        animator.SetBool("isStunned", false);
    }

    [Rpc(SendTo.Server)]
    public void ParalyzeFlashServerRpc(float duration)
    {
        StartCoroutine(Paralyze(duration));
    }

    private IEnumerator Paralyze(float duration)
    {
        isParalyzed = true;
        animator.SetBool("isParalyzed", true);

        yield return new WaitForSeconds(duration);

        isParalyzed = false;
        animator.SetBool("isParalyzed", false);
    }
}
