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
    public AudioSource audioSource;

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

        UpdateVacuumChargeUI();
        UpdateVacuumWindLine();

        if (triggerAction.action.IsPressed() && vacuumCharge > 0)
        {
            Debug.Log("Trigger Button pressed! Is vacuuming...");
            isRecharging = false;
            
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
            HideVacuumWindLine();

            if (isVacuuming)
            {
                StopVacuuming();
            }
        }
    }

    void UpdateVacuumWindLine()
    {
        vacuumWindLine.SetPosition(0, vacuumPoint.position);
        vacuumWindLine.SetPosition(1, transform.position + transform.forward * vacuumRange);
        vacuumWindLine.enabled = true;
    }

    void UpdateVacuumWindLine(RaycastHit hit)
    {
        vacuumWindLine.SetPosition(0, vacuumPoint.position); 
        vacuumWindLine.SetPosition(1, hit.point);
        vacuumWindLine.enabled = true;
    }

    void HideVacuumWindLine()
    {
        vacuumWindLine.enabled = false;
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
        audioSource.Play();

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
}
