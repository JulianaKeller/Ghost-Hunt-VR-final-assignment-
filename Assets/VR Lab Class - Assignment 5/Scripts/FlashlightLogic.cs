using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class FlashlightLogic : NetworkBehaviour
{
    [Header("Flashlight Settings")]
    public float flashRange = 100f;        // Maximum distance the flash can reach
    public float flashDuration = 0.2f;    // Duration of the flash effect
    public float paralysisDuration = 3f;  // How long ghosts stay paralyzed
    public float cooldownDuration = 1f;

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
        Debug.Log("Flash on.");

        // Raycast in front of the flashlight to detect ghosts
        if (Physics.Raycast(transform.position, transform.up, out RaycastHit hit, flashRange, ghostLayer))
        {
            Debug.Log("Hit ghost.");

            GeistBewegung ghost = hit.collider.GetComponent<GeistBewegung>();
            if (ghost != null)
            {
                // Change color and paralyze the ghost
                ghost.ChangeColorServerRpc(Color.white, paralysisDuration);
                ghost.ParalyzeFlashServerRpc(paralysisDuration);
            }
        }

        // Flash duration before turning off
        yield return new WaitForSeconds(flashDuration);
        flashlightLight.enabled = false;
        Debug.Log("Flash off.");

        // Small cooldown before allowing another flash
        yield return new WaitForSeconds(cooldownDuration);
        canFlash = true;
    }
}
