using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

public class SpawnCaptureBall : MonoBehaviour
{
    public GameObject captureBallPrefab;

    private GameObject currentCaptureBall;
    public Transform spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        SpawnNewBall();
    }

    // Update is called once per frame
    void Update()
    {

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
                followScript.SetFollowTarget(spawnPoint);
            }
        }
    }

    void OnBallGrabbed(SelectExitEventArgs args)
    {
        currentCaptureBall = null;
        Invoke("SpawnNewBall", 1f); // Delay before spawning a new ball
    }
}
