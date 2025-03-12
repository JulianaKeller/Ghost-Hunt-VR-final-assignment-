using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnCaptureBall : NetworkBehaviour
{
    public GameObject captureBallPrefab;

    private GameObject currentCaptureBall;
    public Transform spawnPoint;

    private void OnEnable()
    {
        if (IsServer) // Ensure only the server spawns the ball
        {
            Debug.Log("Spawning new Capture Ball...");
            SpawnNewBall();
        }
    }

    private void OnDisable()
    {
        if (IsServer) // Ensure only the server despawns the ball
        {
            DespawnCurrentBall();
        }
    }

    void SpawnNewBall()
    {
        if (currentCaptureBall == null) // Ensure only one ball at a time
        {
            currentCaptureBall = Instantiate(captureBallPrefab, spawnPoint.position, spawnPoint.rotation);
            var ballNetworkObject = currentCaptureBall.GetComponent<NetworkObject>();
            ballNetworkObject.Spawn();

            Debug.Log("New Capture Ball spawned!");

            // Get the follow script from the capture ball
            CaptureBallFollowMode followScript = currentCaptureBall.GetComponent<CaptureBallFollowMode>();
            if (followScript != null)
            {
                // Pass the instance's spawnPoint to the capture ball
                followScript.AttachBall(spawnPoint);
                followScript.SetSpawner(this);
                Debug.Log("Capture Ball attached to spawner");
            }
        }
    }

    void DespawnCurrentBall()
    {
        if (currentCaptureBall != null)
        {
            NetworkObject ballNetworkObject = currentCaptureBall.GetComponent<NetworkObject>();
            if (ballNetworkObject != null && ballNetworkObject.IsSpawned)
            {
                ballNetworkObject.Despawn();
            }
            Destroy(currentCaptureBall); // Ensure the object is also destroyed locally
            currentCaptureBall = null;
        }
    }

    // Called when the ball is picked up
    public void OnBallPickedUp()
    {
        currentCaptureBall = null;
        Invoke(nameof(SpawnNewBall), 1f); // Spawn a new ball after a delay
    }
}
