using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

public class PlasmaGunLogic : NetworkBehaviour
{
    [Header("Plasma Gun Settings")]
    
    public float laserRange = 10f;
    public float maxDuration = 10f;
    public float rechargeTime = 5f;

    [Header("VR Input")]
    public InputActionProperty triggerAction;

    [Header("References")]
    public LayerMask ghostLayer;
    public LineRenderer laserLine;
    public LineRenderer plasmaLine;

    private bool isFiring = false;
    private bool isRecharging = false;
    private Transform targetGhost = null;

    // Network Variables to sync laser line positions
    private NetworkVariable<Vector3> laserStartPos = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> laserEndPos = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> laserEnabled = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> plasmaEnabled = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        maxDuration = NetworkVariableManager.Instance.GetDifficultyProperties().StunDuration;
        rechargeTime = NetworkVariableManager.Instance.GetDifficultyProperties().LaserRechargeDuration;
    }

    void Update()
    {
        if (isRecharging) return;

        if (!isFiring && triggerAction.action.IsPressed())
        {
            maxDuration = NetworkVariableManager.Instance.GetDifficultyProperties().StunDuration;
            rechargeTime = NetworkVariableManager.Instance.GetDifficultyProperties().LaserRechargeDuration;
            StartFiring();
        }
        else if (triggerAction.action.WasReleasedThisFrame())
        {
            StopFiring();
        }

        if (isFiring)
        {
            UpdateLaser();
        }

        // Sync the laser positions across clients
        if (IsServer)
        {
            laserStartPos.Value = laserLine.GetPosition(0);
            laserEndPos.Value = laserLine.GetPosition(1);
        }
    }

    void StartFiring()
    {
        isFiring = true;
        laserLine.enabled = true;
        plasmaLine.enabled = false;

        if (IsServer)
        {
            laserEnabled.Value = true;
            plasmaEnabled.Value = false;
            SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
        }
    }

    void StopFiring()
    {
        isFiring = false;
        laserLine.enabled = false;
        plasmaLine.enabled = false;

        if (IsServer)
        {
            laserEnabled.Value = false;
            plasmaEnabled.Value = false;
            SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
        }

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

        if (Physics.Raycast(laserStart, laserDirection, out hit, laserRange, ghostLayer))
        {
            laserLine.SetPosition(1, hit.point);
            plasmaLine.SetPosition(1, hit.point);

            // Plasma Linie aktivieren, normalen Laser ausblenden
            laserLine.enabled = false;
            plasmaLine.enabled = true;

            if (IsServer)
            {
                laserEnabled.Value = false;
                plasmaEnabled.Value = true;
                SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
            }

            if (targetGhost == null)
            {
                targetGhost = hit.transform;
                targetGhost.GetComponent<GeistBewegung>().StunLaserServerRpc();
                StartCoroutine(LaserDuration());
            }
        }
        else
        {
            if (targetGhost)
            {
                targetGhost.GetComponent<GeistBewegung>().UnstunLaserServerRpc();
                targetGhost = null;
            }
            laserLine.enabled = true;
            plasmaLine.enabled = false;

            if (IsServer)
            {
                laserEnabled.Value = true;
                plasmaEnabled.Value = false;
                SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
            }

            laserLine.SetPosition(1, laserStart + laserDirection * laserRange);
        }
    }

    IEnumerator LaserDuration()
    {
        yield return new WaitForSeconds(maxDuration);
        StopFiring();
        StartCoroutine(RechargeLaser());
    }

    IEnumerator RechargeLaser()
    {
        isRecharging = true;
        yield return new WaitForSeconds(rechargeTime);
        isRecharging = false;
    }

    // will be called when any of the laser positions change
    [ClientRpc]
    private void SyncLaserLineClientRpc(Vector3 start, Vector3 end)
    {
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
        plasmaLine.SetPosition(0, start);
        plasmaLine.SetPosition(1, end);
    }

    // Sync laser on/off state across clients
    [ClientRpc]
    private void SyncLaserStateClientRpc(bool laserActive, bool plasmaActive)
    {
        laserLine.enabled = laserActive;
        plasmaLine.enabled = plasmaActive;
    }
}
