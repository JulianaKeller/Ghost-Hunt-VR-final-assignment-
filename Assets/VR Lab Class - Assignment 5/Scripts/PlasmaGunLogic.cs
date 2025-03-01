using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

public class PlasmaGunLogic : MonoBehaviour
{
    [Header("Plasma Gun Settings")]
    
    public float laserRange = 10f;        // Maximum distance the flash can reach
    public float maxDuration = 10f;    // Duration of the  effect
    public float rechargeTime = 5f;
    public float paralysisTime = 3f;
    

    [Header("VR Input")]
    public InputActionProperty triggerAction; // Assign this to the Quest 2 trigger button

    [Header("References")]
    public LayerMask ghostLayer;   // Ensure this is set to the "Ghosts" layer in the Inspector
    public LineRenderer laserLine;
    public LineRenderer plasmaLine;

    private bool isFiring = false;
    private bool isRecharging = false;
    private Transform targetGhost = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isRecharging) return;

        if (triggerAction.action.IsPressed()) // Trigger gedrückt
        {
            StartFiring();
        }
        else if (triggerAction.action.WasReleasedThisFrame()) // Trigger losgelassen
        {
            StopFiring();
        }

        if (isFiring)
        {
            UpdateLaser();
        }
    }

    void StartFiring()
    {
        isFiring = true;
        laserLine.enabled = true;
        plasmaLine.enabled = false;
    }

    void StopFiring()
    {
        isFiring = false;
        laserLine.enabled = false;
        plasmaLine.enabled = false;

        if (targetGhost)
        {
            targetGhost.GetComponent<GeistBewegung>().UnstunLaserServerRpc();
            targetGhost = null;
        }
    }

    void UpdateLaser()
    {
        RaycastHit hit;
        Vector3 laserStart = transform.position;
        Vector3 laserDirection = transform.forward;

        laserLine.SetPosition(0, laserStart);
        plasmaLine.SetPosition(0, laserStart);

        if (Physics.Raycast(laserStart, laserDirection, out hit, laserRange, LayerMask.GetMask("Ghosts")))
        {
            laserLine.SetPosition(1, hit.point);
            plasmaLine.SetPosition(1, hit.point);

            // Plasma Linie aktivieren, normalen Laser ausblenden
            laserLine.enabled = false;
            plasmaLine.enabled = true;

            if (targetGhost == null)
            {
                targetGhost = hit.transform;
                targetGhost.GetComponent<GeistBewegung>().StunLaserServerRpc();
                StartCoroutine(LaserDuration());
            }
        }
        else
        {
            laserLine.enabled = true;
            plasmaLine.enabled = false;
            laserLine.SetPosition(1, laserStart + laserDirection * laserRange);
        }
    }

    IEnumerator LaserDuration()
    {
        yield return new WaitForSeconds(paralysisTime);
        StopFiring();
        StartCoroutine(RechargeLaser());
    }

    IEnumerator RechargeLaser()
    {
        isRecharging = true;
        yield return new WaitForSeconds(rechargeTime);
        isRecharging = false;
    }
}
