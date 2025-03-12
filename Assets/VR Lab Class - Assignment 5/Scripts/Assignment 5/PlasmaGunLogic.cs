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

    private bool gunIsActive = false;
    private bool isFiring = false;
    private bool isRecharging = false;
    private Transform targetGhost = null;
    private float laserGunCharge;

    private Coroutine rechargeCoroutine;
    private Coroutine dechargeCoroutine;

    // Network Variables to sync laser line positions and state
    private NetworkVariable<Vector3> laserStartPos =
        new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<Vector3> laserEndPos =
        new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> laserEnabled =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> plasmaEnabled =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);




    void OnEnable()
    {
        gunIsActive = true;

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
        gunIsActive = false;
        rechargeCoroutine = null;
        dechargeCoroutine = null;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        HideLaserLine();
        HidePlasmaLine();

        maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
        laserGunCharge = maxLaserGunCharge;
        laserDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunDechargeRate;
        laserRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunRechargeRate;
    }

    private void Start()
    {
        laserEnabled.OnValueChanged += (oldValue, newValue) => laserLine.enabled = newValue;
        plasmaEnabled.OnValueChanged += (oldValue, newValue) => plasmaLine.enabled = newValue;

        laserStartPos.OnValueChanged += (oldValue, newValue) =>
        {
            laserLine.SetPosition(0, newValue);
            plasmaLine.SetPosition(0, newValue);
        };

        laserEndPos.OnValueChanged += (oldValue, newValue) =>
        {
            laserLine.SetPosition(1, newValue);
            plasmaLine.SetPosition(1, newValue);
        };

        laserEnabled.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue) PlayLaserSound();
            else StopLaserSound();
        };

        HideLaserLine();
        HidePlasmaLine();

        maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
        laserGunCharge = maxLaserGunCharge;
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

        if (gunIsActive)
        {
            if (!isFiring && triggerAction.action.IsPressed() && laserGunCharge > 0)
            {
                isFiring = true;
                isRecharging = false;
                maxLaserGunCharge = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunMaxCharge;
                laserDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunDechargeRate;
                laserRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().LaserGunRechargeRate;
                StartFiring();
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
        if (IsServer) laserEnabled.Value = false;
    }

    void HidePlasmaLine()
    {
        plasmaLine.enabled = false;
        if (IsServer) plasmaEnabled.Value = false;
    }

    void showLaserLine()
    {
        laserLine.enabled = true;
        if (IsServer) laserEnabled.Value = true;
    }

    void showPlasmaLine()
    {
        plasmaLine.enabled = true;
        if (IsServer) plasmaEnabled.Value = true;
    }

    private void PlayLaserSound()
    {
        if (laserAudioSource != null && !laserAudioSource.isPlaying)
        {
            laserAudioSource.Play();
            laserAudioSource.loop = true;
        }
    }

    private void StopLaserSound()
    {
        if (laserAudioSource != null && laserAudioSource.isPlaying)
        {
            laserAudioSource.Stop();
        }
    }

    void StartFiring()
    {
        showLaserLine();
        HidePlasmaLine();
    }

    void StopFiring()
    {
        HideLaserLine();
        HidePlasmaLine();

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

        if (IsServer)
        {
            laserStartPos.Value = laserLine.GetPosition(0);
        }

        if (Physics.Raycast(laserStart, laserDirection, out hit, laserRange, ghostLayer))
        {
            laserLine.SetPosition(1, hit.point);
            plasmaLine.SetPosition(1, hit.point);

            if (IsServer)
            {
                laserEndPos.Value = laserLine.GetPosition(1);
            }

            // Plasma Linie aktivieren, normalen Laser ausblenden
            HideLaserLine();
            showPlasmaLine();

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
            showLaserLine();
            HidePlasmaLine();

            laserLine.SetPosition(1, laserStart + laserDirection * laserRange);
        }

        if (IsServer)
        {
            laserEndPos.Value = laserLine.GetPosition(1);
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
                Debug.Log("Decharge Rate: " + laserDechargeRate);
                Debug.Log("Max Charge: " + maxLaserGunCharge);
                Debug.Log("Laser charge decreased to " + laserGunCharge);
            }
            yield return new WaitForSeconds(1f);
        }
    }
}
