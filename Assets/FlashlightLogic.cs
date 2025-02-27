using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class FlashlightLogic : NetworkBehaviour
{
    [Header("Flashlight Settings")]
    public float flashRange = 10f;        // Maximum distance the flash can reach
    public float flashDuration = 0.2f;    // Duration of the flash effect
    public float paralysisDuration = 3f;  // How long ghosts stay paralyzed

    [Header("VR Input")]
    public InputActionProperty triggerAction; // Assign this to the Quest 2 trigger button

    [Header("References")]
    public Light flashlightLight; // Assign the flashlight's Light component
    public LayerMask ghostLayer;   // Ensure this is set to the "Ghosts" layer in the Inspector

    private bool canFlash = true; // Cooldown to prevent spamming

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (triggerAction.action.WasPressedThisFrame() && canFlash)
        {
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        canFlash = false;

        // Enable the flashlight effect
        flashlightLight.enabled = true;

        // Raycast in front of the flashlight to detect ghosts
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, flashRange, ghostLayer))
        {
            if (hit.collider.CompareTag("Ghost"))
            {
                // Try to get the Ghost script and paralyze it
                    hit.collider.GetComponent<GhostBehaviour>().ParalyzeServerRpc(paralysisDuration);
            }
        }

        // Flash duration before turning off
        yield return new WaitForSeconds(flashDuration);
        flashlightLight.enabled = false;

        // Small cooldown before allowing another flash
        yield return new WaitForSeconds(1f);
        canFlash = true;
    }
}
