using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CaptureBallLogic : NetworkBehaviour
{
    public float hoverTimeAfterHit = 3f;
    public float ghostCaptureAnimationDuration = 3f;
    public LayerMask ghostLayer;

    private Rigidbody rb;
    private bool hasBeenThrown = false;
    private bool hasCaptured = false;

    private SpawnGhosts GhostSpawner;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        GhostSpawner = FindObjectOfType<SpawnGhosts>();
        if(GhostSpawner == null)
        {
            Debug.Log("No GhostSpawner found!");
        }

        // Make sure gravity is disabled when the ball spawns
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void Throw()
    {
        hasBeenThrown = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasCaptured) return; // Prevent multiple captures

        if (other.gameObject.CompareTag("Ghost"))
        {
            Debug.Log("CaptureBall collided with a ghost");
            GameObject ghost = other.gameObject;

            if (ghost != null && ghost.GetComponent<GeistBewegung>().IsStunned()) // Only capture if paralyzed
            {
                Debug.Log("Ghost is stunned. CaptureBall is capturing ghost...");
                CaptureGhost(ghost);
            }
        }
    }

    void CaptureGhost(GameObject ghost)
    {

        hasCaptured = true;

        //NetworkVariableManager.Instance.IncrementCaughtGhosts();

        // Stop ball movement and make it hover
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        GeistBewegung geistScript = ghost.GetComponent<GeistBewegung>();
        if (geistScript != null)
        {
            geistScript.Capture(); //Increments Captured Ghosts count network variable
        }

        if (IsOwner)
        {
            SetGhostCapturedServerRpc(ghost.GetComponent<NetworkObject>());
        }

        StartCoroutine(AnimateGhostCapture(ghost));

        // Destroy the ball after a delay of hoverTimeAfterHit
        if (IsServer)
        {
            GhostSpawner.RequestDespawnGhost(ghost);
            StartCoroutine(DespawnCaptureBallAfterDelay());
        }
    }

    IEnumerator AnimateGhostCapture(GameObject ghost)
    {
        //Wait for hit animation to play
        yield return new WaitForSeconds(1.2f);

        //Play ghost capture sounds
        AudioSource[] audioSources = transform.GetComponents<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource != null)
            {
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("AudioSource component is missing!");
            }
        }

        float elapsedTime = 0f;
        Vector3 originalScale = ghost.transform.localScale;
        Vector3 originalPosition = ghost.transform.position;
        Vector3 targetPosition = transform.position; // Position of the capture ball

        while (elapsedTime < ghostCaptureAnimationDuration)
        {
            // Animate position (move towards capture ball)
            ghost.transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / ghostCaptureAnimationDuration);

            // Animate scale (shrink to 0)
            ghost.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsedTime / ghostCaptureAnimationDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure ghost has completely shrunk and moved to the capture ball's position
        ghost.transform.position = targetPosition;
        ghost.transform.localScale = Vector3.zero;
    }

    IEnumerator DespawnCaptureBallAfterDelay()
    {
        yield return new WaitForSeconds(hoverTimeAfterHit);

        NetworkObject ballNetworkObject = transform.GetComponent<NetworkObject>();
        if (ballNetworkObject != null && ballNetworkObject.IsSpawned)
        {
            ballNetworkObject.Despawn();
        }
        Destroy(this); // Ensure the object is also destroyed locally
    }

    [ServerRpc(RequireOwnership = false)]
    void SetGhostCapturedServerRpc(NetworkObjectReference ghostObjectRef)
    {
        if (ghostObjectRef.TryGet(out NetworkObject ghostNetworkObject))
        {
            Animator ghostAnimator = ghostNetworkObject.gameObject.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isHit", true);
            }
        }

        SetGhostCapturedClientRpc(ghostObjectRef);
    }

    [ClientRpc]
    void SetGhostCapturedClientRpc(NetworkObjectReference ghostObjectRef)
    {
        if (ghostObjectRef.TryGet(out NetworkObject ghostNetworkObject))
        {
            Animator ghostAnimator = ghostNetworkObject.gameObject.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isHit", true);
            }
        }
    }
}
