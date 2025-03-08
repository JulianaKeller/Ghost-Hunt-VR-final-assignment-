using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnCaptureBall : NetworkBehaviour
{
    public GameObject captureBallPrefab;

    private GameObject currentCaptureBall;
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (IsServer) // Ensure only the server spawns the ball
        {
            SpawnNewBall();
        }
    }

    void SpawnNewBall()
    {
        if (currentCaptureBall == null) // Ensure only one ball at a time
        {
            currentCaptureBall = Instantiate(captureBallPrefab, spawnPoint.position, spawnPoint.rotation);
            var ballNetworkObject = currentCaptureBall.GetComponent<NetworkObject>();
            ballNetworkObject.Spawn();

            // Get the follow script from the capture ball
            CaptureBallFollowMode followScript = currentCaptureBall.GetComponent<CaptureBallFollowMode>();
            if (followScript != null)
            {
                // Pass the instance's spawnPoint to the capture ball
                followScript.AttachBall(spawnPoint);
                followScript.SetSpawner(this);
            }
        }
    }

    // Called when the ball is picked up
    public void OnBallPickedUp()
    {
        currentCaptureBall = null;
        Invoke(nameof(SpawnNewBall), 1f); // Spawn a new ball after a delay
    }
}
