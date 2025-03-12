using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CaptureBallFollowMode : NetworkBehaviour
{
    private Transform followTarget;
    private SpawnCaptureBall spawner;

    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;

    private NetworkVariable<bool> isAttached = new NetworkVariable<bool>(true);

    void Update()
    {
        // When attached and a valid target is assigned, update transform
        if (IsServer && isAttached.Value && followTarget != null)
        {
            transform.position = followTarget.position + positionOffset;
            transform.rotation = followTarget.rotation;
        }
    }

    // Call this method when player picks up the ball
    public void DetachBall()
    {
        if (!IsServer) return; // Only the server should handle detachment

        isAttached.Value = false;

        // Notify the spawner that the ball is picked up
        if (spawner != null)
        {
            spawner.OnBallPickedUp();
        }
    }

    // reattach or set a new follow target
    public void AttachBall(Transform newFollowTarget)
    {
        if (!IsServer) return; // Only the server should reattach

        followTarget = newFollowTarget;
        isAttached.Value = true;
    }

    public void SetFollowTarget(Transform newTarget)
    {
        followTarget = newTarget;
    }

    public void SetSpawner(SpawnCaptureBall spawnerReference)
    {
        spawner = spawnerReference;
    }
}
