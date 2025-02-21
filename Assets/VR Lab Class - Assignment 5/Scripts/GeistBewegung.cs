using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeistBewegung : MonoBehaviour
{
    public float speed = 5f; // Bewegungsgeschwindigkeit
    public float minRotationAngleBoundary = 90f; // Maximale Rotationswinkel
    public float maxRotationAngleBoundary = 270f;
    public float maxRotationAngleRandomRotation = 90f; // Maximale Rotationswinkel
    public float rotationChanceIncreaseRate = 0.1f; // Erhöhungsrate der Rotationswahrscheinlichkeit
    public LayerMask Boundary; // Layer für Begrenzungsobjekte
    public LayerMask Ghosts; // Layer für Begrenzungsobjekte

    private float timeSinceLastTurn = 0f;
    private float rotationChance = 0f;

    void Start()
    {

    }

    void Update()
    {
        MoveForward();
        IncreaseRotationChance();
        TryRandomRotation();
    }

    void MoveForward()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
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
