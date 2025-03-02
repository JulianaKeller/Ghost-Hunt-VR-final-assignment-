using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SpawnCaptureBall : MonoBehaviour
{
    public GameObject captureBallPrefab;

    private GameObject currentCaptureBall;
    public Transform spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        spawnPoint = this.transform;
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
            XRGrabInteractable grabInteractable = currentCaptureBall.GetComponent<XRGrabInteractable>();

            if (grabInteractable != null)
            {
                grabInteractable.selectExited.AddListener(OnBallGrabbed);
            }
        }
    }

    void OnBallGrabbed(SelectExitEventArgs args)
    {
        currentCaptureBall = null;
        Invoke("SpawnNewBall", 1f); // Delay before spawning a new ball
    }
}
