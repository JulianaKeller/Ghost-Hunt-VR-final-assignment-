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
    public float laserDechargeRate = 1f;
    public float laserRechargeRate = 1f;
    public float maxLaserGunCharge = 5;

    [Header("VR Input")]
    public InputActionProperty triggerAction;

    [Header("References")]
    public LayerMask ghostLayer;
    public LineRenderer laserLine;
    public LineRenderer plasmaLine;
    public AudioSource laserAudioSource;

    private bool enabled = false;
    private bool isFiring = false;
    private bool isRecharging = false;
    private Transform targetGhost = null;
    private float laserGunCharge;

    private Coroutine rechargeCoroutine;
    private Coroutine dechargeCoroutine;

    // Network Variables to sync laser line positions
    private NetworkVariable<Vector3> laserStartPos = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> laserEndPos = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> laserEnabled = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> plasmaEnabled = new NetworkVariable<bool>(false);

    

    void OnEnable()
    {
        enabled = true;

        if (rechargeCoroutine == null)
        {
            rechargeCoroutine = StartCoroutine(RechargeLaser());
        }

        if (dechargeCoroutine == null)
        {
            dechargeCoroutine = StartCoroutine(DechargeLaser());
        }
    }

    void OnDisable()
    {
        Debug.Log("LaserGunLogic disabled! Stopping coroutines.");
        StopAllCoroutines();
        isFiring = false;
        isRecharging = false;
        laserEnabled.Value = false;
        plasmaEnabled.Value = false;
        enabled = false;
        rechargeCoroutine = null;
        dechargeCoroutine = null;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        HideLaserLine();
        HidePlasmaLine();

        if (IsClient)
        {
            SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
            SyncLaserLineClientRpc(laserStartPos.Value, laserEndPos.Value);
        }

        maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
        laserDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunDechargeRate;
        laserRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunRechargeRate;
    }

    private void Start()
    {
        laserEnabled.OnValueChanged += (oldValue, newValue) => laserLine.enabled = newValue;
        plasmaEnabled.OnValueChanged += (oldValue, newValue) => plasmaLine.enabled = newValue;

        maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
        laserDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunDechargeRate;
        laserRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunRechargeRate;

        if (rechargeCoroutine == null)
        {
            rechargeCoroutine = StartCoroutine(RechargeLaser());
        }
        if (dechargeCoroutine == null)
        {
            dechargeCoroutine = StartCoroutine(DechargeLaser());
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (enabled)
        {
            if (isRecharging)
            {
                HideLaserLine();
                HidePlasmaLine();
                return;
            }

            if (!isFiring && triggerAction.action.IsPressed() && laserGunCharge > 0)
            {
                isFiring = true;
                isRecharging = false;
                maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
                laserDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunDechargeRate;
                laserRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunRechargeRate;
                StartFiring();
            }
            else if (triggerAction.action.WasReleasedThisFrame())
            {
                isRecharging = true;
                isFiring = false;
                StopFiring();
            }
            if (triggerAction.action.IsPressed() && laserGunCharge > 0)
            {
                isFiring = true;
                isRecharging = false;
                UpdateLaser();
            }
            else
            {
                isRecharging = true;
                isFiring = false;
                StopFiring();
            }
        }
        
    }

    void HideLaserLine()
    {
        laserLine.enabled = false;
        SyncLaserStateClientRpc(false, false);
    }

    private void StopLaserAudio()
    {
        if (!IsServer) return;
        StopLaserAudioClientRpc();
    }

    private void PlayLaserAudio()
    {
        if (!IsServer) return;
        PlayLaserAudioClientRpc();
    }

    void HidePlasmaLine()
    {
        plasmaLine.enabled = false;
        SyncLaserStateClientRpc(false, false);
    }

    void StartFiring()
    {
        laserLine.enabled = true; //ToDo Networking
        HidePlasmaLine();

        PlayLaserAudio();

        if (IsServer)
        {
            laserEnabled.Value = true;
            plasmaEnabled.Value = false;
            SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
        }
    }

    void StopFiring()
    {
        HideLaserLine();
        HidePlasmaLine();

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
        StopLaserAudio();
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
            HideLaserLine();
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
            HidePlasmaLine();

            if (IsServer)
            {
                laserEnabled.Value = true;
                plasmaEnabled.Value = false;
                SyncLaserStateClientRpc(laserEnabled.Value, plasmaEnabled.Value);
            }

            laserLine.SetPosition(1, laserStart + laserDirection * laserRange);
        }

        if (IsServer)
        {
            laserStartPos.Value = laserLine.GetPosition(0);
            laserEndPos.Value = laserLine.GetPosition(1);
            SyncLaserLineClientRpc(laserStartPos.Value, laserEndPos.Value);
        }
    }

    IEnumerator RechargeLaser()
    {
        while (true)
        {
            if (laserGunCharge < maxLaserGunCharge && isRecharging && !isFiring)
            {
            laserGunCharge += laserRechargeRate;
                Debug.Log("Laser charge increased to " + laserGunCharge);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator DechargeLaser()
    {
        while (true)
        {
            if (isFiring && !isRecharging && laserGunCharge > 0)
            {
                laserGunCharge -= laserDechargeRate;
                Debug.Log("Laser charge decreased to " + laserGunCharge);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    #region RCPs

    [ClientRpc]
    private void SyncLaserLineClientRpc(Vector3 start, Vector3 end)
    {
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);
        plasmaLine.SetPosition(0, start);
        plasmaLine.SetPosition(1, end);
    }

    [ClientRpc]
    private void SyncLaserStateClientRpc(bool laserActive, bool plasmaActive)
    {
        laserLine.enabled = laserActive;
        plasmaLine.enabled = plasmaActive;
    }

    [ClientRpc]
    private void PlayLaserAudioClientRpc()
    {
        if (laserAudioSource != null && !laserAudioSource.isPlaying)
        {
            laserAudioSource.Play();
            laserAudioSource.loop = true;
        }
    }

    [ClientRpc]
    private void StopLaserAudioClientRpc()
    {
        if (laserAudioSource != null && laserAudioSource.isPlaying)
        {
            laserAudioSource.Stop();
        }
    }

    #endregion
}
