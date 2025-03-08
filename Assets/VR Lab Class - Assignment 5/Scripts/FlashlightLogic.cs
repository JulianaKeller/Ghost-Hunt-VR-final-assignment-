using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class FlashlightLogic : NetworkBehaviour
{
    [Header("Flashlight Settings")]
    public float flashRange = 100f;
    public float flashDuration = 0.2f;
    public float paralysisDuration = 3f;
    public float cooldownDuration = 1f;

    [Header("VR Input")]
    public InputActionProperty triggerAction;

    [Header("References")]
    public Light flashlightLight;
    public LayerMask ghostLayer;

    private bool canFlash = true;

    public override void OnNetworkSpawn()
    {
        paralysisDuration = NetworkVariableManager.Instance.GetDifficultyProperties().ParalyzeDuration;
        cooldownDuration = NetworkVariableManager.Instance.GetDifficultyProperties().FlashlightCooldownDuration;
    }

    void Update()
    {
        if (!IsOwner) return; // Ensure only the local player triggers flash

        if (triggerAction.action.WasPressedThisFrame() && canFlash)
        {
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        //Make sure variables are up to date
        paralysisDuration = NetworkVariableManager.Instance.GetDifficultyProperties().ParalyzeDuration;
        cooldownDuration = NetworkVariableManager.Instance.GetDifficultyProperties().FlashlightCooldownDuration;

        canFlash = false;

        // Enable the flashlight effect
        FlashLightEffectClientRpc(true);
        //Debug.Log("Flash on.");

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
        FlashLightEffectClientRpc(false);
        //Debug.Log("Flash off.");

        // Cooldown before allowing another flash
        yield return new WaitForSeconds(cooldownDuration);
        canFlash = true;
    }

    [ClientRpc]
    private void FlashLightEffectClientRpc(bool state)
    {
        flashlightLight.enabled = state;
    }
}