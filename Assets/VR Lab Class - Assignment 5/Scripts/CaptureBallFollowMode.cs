using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CaptureBallFollowMode : NetworkBehaviour
{
    private Transform followTarget;

    // Optional: offsets if needed (set in inspector)
    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;
    [SerializeField]
    private Vector3 rotationOffset = Vector3.zero; // In Euler angles

    // Flag to indicate whether the ball is attached to the spawner.
    private bool isAttached = true;

    public void SetFollowTarget(Transform newTarget)
    {
        followTarget = newTarget;
    }

    void Update()
    {
        // When attached and a valid target is assigned, update transform.
        if (isAttached && followTarget != null)
        {
            transform.position = followTarget.position + positionOffset;
            transform.rotation = followTarget.rotation * Quaternion.Euler(rotationOffset);
        }
    }

    // Call this method when the player picks up the ball.
    public void DetachBall()
    {
        isAttached = false;
        // Additional logic here to enable physics, collisions, etc.
    }

    // Optionally, call this to reattach or set a new follow target.
    public void AttachBall(Transform newFollowTarget)
    {
        followTarget = newFollowTarget;
        isAttached = true;
    }
}
