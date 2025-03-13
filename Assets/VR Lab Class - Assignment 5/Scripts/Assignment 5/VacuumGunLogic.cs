using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VacuumGunLogic : NetworkBehaviour
{
    [Header("Vacuum Gun Settings")]
    public float vacuumRange = 10f;
    public float vacuumDechargeRate = 1f;
    public float vacuumRechargeRate = 1f;
    public float maxVacuumCharge = 5;
    public float vibrationDuration = 5f;
    public float suctionDuration = 4f;

    [Header("References")]
    public Transform vacuumPoint;
    public LayerMask ghostLayer;
    public InputActionProperty triggerAction;
    public string ghostTag = "Ghost";
    public AudioSource[] capturedAudioSources;
    public AudioSource vacuumAudioSource;

    [Header("UI Elements")]
    public RawImage vacuumChargeImage;

    [Header("Vacuum Gun Visuals")]
    public LineRenderer vacuumWindLine;

    private bool isVacuuming = false;
    private bool isRecharging = false;
    private float vacuumCharge;
    private Transform targetGhost = null;
    private Coroutine vacuumCoroutine;
    private Coroutine rechargeCoroutine;
    private Coroutine dechargeCoroutine;
    private Vector3 ghostInitialPosition;
    private Vector3 ghostInitialScale;

    private NetworkVariable<Vector3> vacuumLineStart = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> vacuumLineEnd = new NetworkVariable<Vector3>();

    //private NetworkVariable<bool> isGhostVacuuming = new NetworkVariable<bool>(false);

    void OnDisable()
    {
        Debug.Log("VacuumGunLogic disabled! Stopping coroutines.");
        StopAllCoroutines();
        vacuumCoroutine = null;
        rechargeCoroutine = null;
        dechargeCoroutine = null;
    }

    void OnEnable()
    {
        Debug.Log("VacuumGunLogic enabled! Restarting coroutines.");

        if (rechargeCoroutine == null)
        {
            rechargeCoroutine = StartCoroutine(RechargeVacuum());
        }

        if (dechargeCoroutine == null)
        {
            dechargeCoroutine = StartCoroutine(DechargeVacuum());
        }
    }

    void Awake()
    {
        vacuumLineStart.OnValueChanged += (previous, current) => ShowVacuumWindLineClientRpc(vacuumLineStart.Value, vacuumLineEnd.Value);
        vacuumLineEnd.OnValueChanged += (previous, current) => ShowVacuumWindLineClientRpc(vacuumLineStart.Value, vacuumLineEnd.Value);
    }

    void Start()
    {
        HideVacuumWindLine();

        maxVacuumCharge = NetworkVariableManager.Instance.GetDifficultyProperties().VacuumMaxCharge;
        vacuumDechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().VacuumDechargeRate;
        vacuumRechargeRate = NetworkVariableManager.Instance.GetDifficultyProperties().VacuumRechargeRate;

        vacuumCharge = maxVacuumCharge;
        Debug.Log("Vacuum Charge Initialized: " + vacuumCharge);
        vibrationDuration = maxVacuumCharge / 2;

        if (rechargeCoroutine == null)
        {
            rechargeCoroutine = StartCoroutine(RechargeVacuum());
        }
        if (dechargeCoroutine == null)
        {
            dechargeCoroutine = StartCoroutine(DechargeVacuum());
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        UpdateVacuumWindLine();
        UpdateVacuumChargeUI();

        if (triggerAction.action.IsPressed() && vacuumCharge > 0)
        {
            Debug.Log("Trigger Button pressed! Is vacuuming...");
            isRecharging = false;

            //Enable vacuum audio
            if (IsServer)
            {
                PlayVacuumAudioClientRpc();
            }

            if (!isVacuuming)
            {
                isVacuuming = true;
                TryVacuumGhost();
            }
        }
        else
        {
            isVacuuming = false;
            isRecharging = true;

            if (IsServer)
            {
                StopVacuumAudioClientRpc();
            }

            HideVacuumWindLine();

            if (isVacuuming)
            {
                StopVacuuming();
            }
        }
    }

    void UpdateVacuumWindLine()
    {
        Vector3 startPos = vacuumPoint.position;
        Vector3 endPos = transform.position + transform.forward * vacuumRange;

        if (IsOwner)
        {
            vacuumWindLine.SetPosition(0, startPos);
            vacuumWindLine.SetPosition(1, endPos);

            if (isVacuuming)
            {
                ShowVacuumWindLine(startPos, endPos);
            }
            else
            {
                HideVacuumWindLine();
            }
        }
    }

    void UpdateVacuumWindLine(RaycastHit hit)
    {
        Vector3 startPos = vacuumPoint.position;
        Vector3 endPos = hit.point;

        if (IsOwner)
        {
            vacuumWindLine.SetPosition(0, startPos);
            vacuumWindLine.SetPosition(1, endPos);

            if (isVacuuming)
            {
                ShowVacuumWindLine(startPos, endPos);
            }
            else
            {
                HideVacuumWindLine();
            }
        }
    }

    void HideVacuumWindLine()
    {

        vacuumWindLine.enabled = false;
        HideVacuumWindLineServerRpc();
    }

    void ShowVacuumWindLine(Vector3 startPos, Vector3 endPos)
    {

        vacuumWindLine.enabled = true;
        ShowVacuumWindLineServerRpc(startPos, endPos);
    }

    void UpdateVacuumChargeUI()
    {
        if (vacuumChargeImage != null)
        {
            // Calculate the width based on vacuum charge
            float width = Mathf.Lerp(0.3f, 0.0f, 1 - (vacuumCharge / maxVacuumCharge));

            // Set the raw image width
            RectTransform rt = vacuumChargeImage.GetComponent<RectTransform>();
            rt.localScale = new Vector3(rt.localScale.x, width, rt.localScale.z);
        }
    }

    void TryVacuumGhost()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, vacuumRange, ghostLayer))
        {
            UpdateVacuumWindLine(hit);
            if (hit.collider.CompareTag(ghostTag))
            {
                Debug.Log("Vacuum hit ghost...");
                GeistBewegung geistScript = hit.collider.GetComponent<GeistBewegung>();
                if (geistScript != null && geistScript.IsParalyzed()) // Ensure the ghost is paralyzed
                {
                    Debug.Log("Ghost is paralyzed, Vacuuming ghost...");
                    StartVacuuming(hit.collider.transform);
                }
            }
        }
    }

    void StartVacuuming(Transform ghost)
    {
        //isVacuumingNetwork.Value = true;
        targetGhost = ghost;

        ghostInitialPosition = ghost.position;
        ghostInitialScale = ghost.localScale;

        if (vacuumCoroutine == null)
        {
            vacuumCoroutine = StartCoroutine(VacuumSequence(ghost));
        }
    }

    IEnumerator VacuumSequence(Transform ghost)
    {
        float elapsed = 0f;
        bool growing = true;

        while (elapsed < vibrationDuration)
        {
            ghostInitialPosition = ghost.position;

            Debug.Log("Ghost is vibrating...");
            if (targetGhost == null || !targetGhost.GetComponent<GeistBewegung>().IsParalyzed())
            {
                StopVacuuming();
                yield break;
            }

            float scaleModifier = growing ? 1.1f : 0.9f;
            ghost.localScale = ghostInitialScale * scaleModifier;
            growing = !growing;

            if (vacuumCharge <= 0)
            {
                StopVacuuming();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        StartCoroutine(SuckGhost(ghost));
    }

    IEnumerator SuckGhost(Transform ghost)
    {
        if (IsServer)
        {
            PlayCapturedAudioClientRpc();
        }

        ghostInitialPosition = ghost.position;

        float elapsed = 0f;

        if (IsOwner)
        {
            SetGhostVacuumedServerRpc(ghost.GetComponent<NetworkObject>());
        }

        while (elapsed < suctionDuration)
        {
            Debug.Log("Ghost is being sucked in......");
            if (vacuumCharge <= 0)
            {
                StopVacuuming();
                yield break;
            }

            float t = elapsed / suctionDuration;
            ghost.position = Vector3.Lerp(ghostInitialPosition, vacuumPoint.position, t);
            ghost.localScale = Vector3.Lerp(ghostInitialScale, Vector3.zero, t);

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (IsServer)
        {
            ghost.GetComponent<GeistBewegung>().Capture();
            SpawnGhosts.Instance.RequestDespawnGhost(ghost.gameObject);
        }

        ghost.gameObject.SetActive(false);
        StopVacuuming();
    }

    void StopVacuuming()
    {
        isVacuuming = false;

        if (targetGhost != null)
        {
            // Reset ghost position and scale if vacuuming was interrupted
            targetGhost.position = ghostInitialPosition;
            targetGhost.localScale = ghostInitialScale;
        }

        targetGhost = null;

        if (vacuumCoroutine != null)
        {
            StopCoroutine(vacuumCoroutine);
            vacuumCoroutine = null;
        }
    }

    private void StopVacuumAudio()
    {
        if (vacuumAudioSource != null && vacuumAudioSource.isPlaying)
        {
            vacuumAudioSource.Stop();
        }
        else
        {
            Debug.LogWarning("An AudioSource component is missing!");
        }
    }

    IEnumerator RechargeVacuum()
    {
        while (true)
        {
            if(vacuumCharge < maxVacuumCharge && isRecharging && !isVacuuming)
            {
                vacuumCharge += vacuumRechargeRate;
                Debug.Log("Vacuum charge increased to " + vacuumCharge);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator DechargeVacuum()
    {
        while (true)
        {
            if (isVacuuming && !isRecharging && vacuumCharge > 0)
            {
                vacuumCharge -= vacuumDechargeRate;
                Debug.Log("Vacuum charge decreased to " + vacuumCharge);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    #region RCPs

    [ServerRpc(RequireOwnership = false)]
    void SetGhostVacuumedServerRpc(NetworkObjectReference ghostObjectRef)
    {
        if (ghostObjectRef.TryGet(out NetworkObject ghostNetworkObject))
        {
            Animator ghostAnimator = ghostNetworkObject.gameObject.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isVacuumed", true);
            }
        }

        SetGhostVacuumedClientRpc(ghostObjectRef);
    }

    [ClientRpc]
    void SetGhostVacuumedClientRpc(NetworkObjectReference ghostObjectRef)
    {
        if (ghostObjectRef.TryGet(out NetworkObject ghostNetworkObject))
        {
            Animator ghostAnimator = ghostNetworkObject.gameObject.GetComponent<Animator>();
            if (ghostAnimator != null)
            {
                ghostAnimator.SetBool("isVacuumed", true);
            }
        }
    }

    [ClientRpc]
    void PlayVacuumAudioClientRpc()
    {
        if (vacuumAudioSource != null && !vacuumAudioSource.isPlaying)
        {
            vacuumAudioSource.Play();
        }
    }

    [ClientRpc]
    void StopVacuumAudioClientRpc()
    {
        if (vacuumAudioSource != null && vacuumAudioSource.isPlaying)
        {
            vacuumAudioSource.Stop();
        }
    }

    [ClientRpc]
    void PlayCapturedAudioClientRpc()
    {
        if (capturedAudioSources != null)
        {
            foreach (var audioSource in capturedAudioSources)
            {
                if (audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
        }
    }

    [ServerRpc]
    void ShowVacuumWindLineServerRpc(Vector3 start, Vector3 end)
    {
        vacuumLineStart.Value = start;
        vacuumLineEnd.Value = end;
        vacuumWindLine.enabled = true;

        ShowVacuumWindLineClientRpc(start, end);
    }

    [ClientRpc]
    void ShowVacuumWindLineClientRpc(Vector3 start, Vector3 end)
    {
        vacuumWindLine.SetPosition(0, start);
        vacuumWindLine.SetPosition(1, end);
        vacuumWindLine.enabled = true;
    }


    [ServerRpc]
    void HideVacuumWindLineServerRpc()
    {
        vacuumWindLine.enabled = false;
        HideVacuumWindLineClientRpc();
    }

    [ClientRpc]
    void HideVacuumWindLineClientRpc()
    {
        vacuumWindLine.enabled = false;
    }


    #endregion
}
