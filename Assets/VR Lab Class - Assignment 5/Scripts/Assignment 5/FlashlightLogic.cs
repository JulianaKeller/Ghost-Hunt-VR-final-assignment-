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

    private NetworkVariable<bool> isFlashlightOn = new NetworkVariable<bool>(false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    void Start()
    {

        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;
    }

    private void OnFlashlightStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log("On Flashlight Value changed...");
        flashlightLight.enabled = newValue;
    }

    public override void OnNetworkSpawn()
    {
        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;
        paralysisDuration = NetworkVariableManager.Instance.GetDifficultyProperties().ParalyzeDuration;
        cooldownDuration = NetworkVariableManager.Instance.GetDifficultyProperties().FlashlightCooldownDuration;

    }

    void OnEnable()
    {
        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;
        canFlash = true;

        paralysisDuration = NetworkVariableManager.Instance.GetDifficultyProperties().ParalyzeDuration;
        cooldownDuration = NetworkVariableManager.Instance.GetDifficultyProperties().FlashlightCooldownDuration;
    }

    void OnDisable()
    {
        canFlash = false;
    }

    void Update()
    {
        if (triggerAction.action.WasPressedThisFrame() && canFlash)
        {
            //Debug.Log("Flashing...");
            StartCoroutine(Flash());
        }
    }

    private IEnumerator Flash()
    {
        //Make sure variables are up to date
        paralysisDuration = NetworkVariableManager.Instance.GetDifficultyProperties().ParalyzeDuration;
        cooldownDuration = NetworkVariableManager.Instance.GetDifficultyProperties().FlashlightCooldownDuration;

        canFlash = false;

        if (IsOwner)
        {
            //FlashLightEffectClientRpc(true); // will be called on all clients, including the owner
        }
        flashlightLight.enabled = true;
        if (IsServer)
        {
            
            isFlashlightOn.Value = true;
        }
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

        yield return new WaitForSeconds(flashDuration);

        if (IsOwner)
        {
            //FlashLightEffectClientRpc(false);
        }
        flashlightLight.enabled = false;
        if (IsServer)
        {
            isFlashlightOn.Value = false;
        }
        //Debug.Log("Flash off.");

        // Cooldown before allowing another flash
        yield return new WaitForSeconds(cooldownDuration);
        canFlash = true;
    }

    /*[ClientRpc]
    private void FlashLightEffectClientRpc(bool state)
    {
        flashlightLight.enabled = state;
        
        Debug.Log($"Flashlight toggled: " + state);
    }*/
}