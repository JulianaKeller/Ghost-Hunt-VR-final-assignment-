using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CaptureBallLogic : NetworkBehaviour
{
    public float hoverTimeAfterHit = 2f;
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

    void OnCollisionEnter(Collision collision)
    {
        if (hasCaptured) return; // Prevent multiple captures

        if (collision.gameObject.CompareTag("Ghost"))
        {
            Debug.Log("CaptureBall collided with a ghost");
            GameObject ghost = collision.gameObject;

            if (ghost != null && ghost.GetComponent<GeistBewegung>().IsParalyzed()) // Only capture if paralyzed
            {
                Debug.Log("Ghost is paralyized. CaptureBall is capturing ghost...");
                CaptureGhost(ghost);
            }
        }
    }

    void CaptureGhost(GameObject ghost)
    {

        hasCaptured = true;

        NetworkVariableManager.Instance.IncrementCaughtGhosts();

        // Stop ball movement and make it hover
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        //Call Capture on ghost
        GeistBewegung geistScript = ghost.GetComponent<GeistBewegung>();
        if (geistScript != null)
        {
            geistScript.Capture();
        }

        // Destroy the ball after a delay of hoverTimeAfterHit
        if (IsServer)
        {
            StartCoroutine(DespawnGhostAfterDelay(ghost));
        }
    }

    IEnumerator DespawnGhostAfterDelay(GameObject ghost)
    {
        yield return new WaitForSeconds(hoverTimeAfterHit);

        GhostSpawner.DespawnGhost(ghost);
    }
}
