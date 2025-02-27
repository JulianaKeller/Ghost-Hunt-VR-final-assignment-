using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlasmaGunLogic : MonoBehaviour
{
    [Header("Plasma Gun Settings")]
    public float laserRange = 10f;        // Maximum distance the flash can reach
    public float maxDuration = 0.2f;    // Duration of the flash effect
    public float cooldownDuration = 1f;

    [Header("VR Input")]
    public InputActionProperty triggerAction; // Assign this to the Quest 2 trigger button

    [Header("References")]
    public LayerMask ghostLayer;   // Ensure this is set to the "Ghosts" layer in the Inspector

    private bool canTurnOn = true; // Cooldown to prevent spamming

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (triggerAction.action.IsPressed() && canTurnOn)
        {
            StartCoroutine(Laser());
        }
        if (triggerAction.action.WasReleasedThisFrame())
        {
            //disable laser
            //stop coroutine
        }
    }

    private IEnumerator Laser()
    {
        canTurnOn = false;

        //enable laser

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, laserRange, ghostLayer))
        {
            if (hit.collider.CompareTag("Ghost")) //ToDo check tag
            {
                // Paralyze the ghost while the ghost is hit by the raycast
            }
        }

        yield return new WaitForSeconds(maxDuration);
        //disable laser

        yield return new WaitForSeconds(cooldownDuration); //maybe this should be a recharging time instead
        canTurnOn = true;
    }
}
