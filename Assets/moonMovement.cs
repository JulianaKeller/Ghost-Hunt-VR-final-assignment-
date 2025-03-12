using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class moonMovement : NetworkBehaviour
{
    public Vector3 startPosition;
    public Vector3 endPosition;
    public float duration = 300f;
    private float timeNormalized;
    private Vector3 currentVelocity = Vector3.zero;
    public float smoothTime = 0.5f; // Adjust for smoothness

    void Start()
    {
        duration = NetworkVariableManager.Instance.GetGameTimeLimit();
        startPosition = transform.position;
        endPosition = new Vector3(startPosition.x, -27, startPosition.z);
    }

    void Update()
    {
        duration = NetworkVariableManager.Instance.GetGameTimeLimit();
        float elapsedTime = NetworkVariableManager.Instance.GetGameTime();
        timeNormalized = elapsedTime / duration;
        Vector3 targetPosition = Vector3.Lerp(startPosition, endPosition, timeNormalized);

        //Smooth movement:
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
