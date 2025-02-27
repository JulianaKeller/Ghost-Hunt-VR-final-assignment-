using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostRootMovement : MonoBehaviour
{
    public Transform ghostModel; // The GhostModel inside GhostRoot
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
    private Vector3 lastPosition;

    void Start()
    {
        animator = ghostModel.GetComponent<Animator>();
        lastPosition = ghostModel.position;

    }

    void Update()
    {
        animator.speed = animationSpeed;

        MoveForward();
        IncreaseRotationChance();
        TryRandomRotation();
    }

    void MoveForward()
    {
        //transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // Adjust GhostRoot's movement based on animation's movement
        // Move the GhostRoot forward by the same distance the GhostModel moved
        Vector3 deltaMovement = ghostModel.position - lastPosition;
        if (deltaMovement.sqrMagnitude < movementSpeedMultiplier)
        {
            transform.position += deltaMovement * movementSpeedMultiplier;

            lastPosition = ghostModel.position;
        }
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
}
