using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GeistBewegung : NetworkBehaviour
{
    [Header("Animation and Movement Settings")]
    public float animationSpeed = 1.0f;
    public float movementSpeedMultiplier = 1.0f; // Bewegungsgeschwindigkeit

    [Header("Rotation Settings")]
    public float minRotationAngleBoundary = 90f; // Maximale Rotationswinkel
    public float maxRotationAngleBoundary = 270f;
    public float maxRotationAngleRandomRotation = 90f; // Maximale Rotationswinkel
    public float rotationChanceIncreaseRate = 0.1f; // Erhöhungsrate der Rotationswahrscheinlichkeit

    [Header("References")]
    public LayerMask Boundary; // Layer für Begrenzungsobjekte
    public LayerMask Ghosts; // Layer für Begrenzungsobjekte
    [SerializeField] private Material ghostMaterial;

    //Rotation values
    private float timeSinceLastTurn = 0f;
    private float rotationChance = 0f;

    //References
    private Animator animator;
    private Color originalColor = Color.black;

    // Network Variables (Synced across all clients)
    private NetworkVariable<bool> isVisible = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isParalyzed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isStunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> walkingV1Chance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> idleChance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        //Debug.Log("Entering onNetworkSpawn...");
        if (IsServer)
        {
            // Start ghost movement only on the server:

            //Randomly change walking animation
            InvokeRepeating(nameof(UpdateWalkingAnimation), 2f, 2f); // Alle 2 Sekunden checken
        }
    }

    void Start()
    {
        //Get the components
        animator = GetComponent<Animator>();
        if (ghostMaterial != null)
        {
            originalColor = ghostMaterial.color;
        }
        else
        {
            Debug.LogError("Ghost material is not assigned!", this);
        }
    }

    void Update()
    {
        if (IsServer) // Only the server should control movement
        {
            if (!isParalyzed.Value && !isStunned.Value && !animator.GetCurrentAnimatorStateInfo(0).IsName("Drunk Idle"))
            {
                MoveForward();
                IncreaseRotationChance();
                TryRandomRotation();
            }
            else
            {
                //Debug.Log("Paralyzed or stunned or idle.");
            }
        } 

        animator.speed = animationSpeed;

        // Al Clients apply the network variable values for the animator variables
        animator.SetBool("isStunned", isStunned.Value);
        animator.SetBool("isParalyzed", isParalyzed.Value);
        animator.SetFloat("walkingV1Chance", walkingV1Chance.Value);
        animator.SetFloat("IdleChance", idleChance.Value);
    }

    void UpdateWalkingAnimation()
    {
        if (!IsServer) return;

        walkingV1Chance.Value = Random.Range(0.0f, 1.0f);
        idleChance.Value = Random.Range(0.0f, 1.0f);
    }

    void MoveForward()
    {
        //Fix random rotation problems where the ghost rotates not around the y axsis for no apparent reason :(
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

        Vector3 forwardDirection = Vector3.forward;
        transform.Translate(new Vector3(forwardDirection.x, 0, forwardDirection.z) * movementSpeedMultiplier * Time.deltaTime);
    }

    #region Rotation methods

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (((1 << other.gameObject.layer) & Boundary) != 0 || ((1 << other.gameObject.layer) & Ghosts) != 0)
        {
            RotateRandomly(minRotationAngleBoundary, maxRotationAngleBoundary);
            ResetRotationChance();
        }
    }

    void IncreaseRotationChance()
    {
        rotationChance += rotationChanceIncreaseRate * Time.deltaTime;
    }

    void TryRandomRotation()
    {
        if (Random.value < rotationChance)
        {
            RotateRandomly(0f, maxRotationAngleRandomRotation);
            ResetRotationChance();
        }
    }

    void ResetRotationChance()
    {
        rotationChance = 0f;
    }

    void RotateRandomly(float minAngle, float maxAngle)
    {
        float randomAngle = Random.Range(minAngle, maxAngle);
        transform.Rotate(Vector3.up, randomAngle);
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server)]
    public void StunLaserServerRpc() //Laser
    {
        isStunned.Value = true;
        animator.SetBool("isStunned", true);
    }

    [Rpc(SendTo.Server)]
    public void UnstunLaserServerRpc() //Laser
    {
        isStunned.Value = false;
        animator.SetBool("isStunned", false);
    }

    [Rpc(SendTo.Server)]
    public void ParalyzeFlashServerRpc(float duration) //Flashlight
    {
        StartCoroutine(Paralyze(duration));
    }

    private IEnumerator Paralyze(float duration) //Flashlight
    {
        isParalyzed.Value = true;
        yield return new WaitForSeconds(duration);
        isParalyzed.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeColorServerRpc(Color newColor, float duration)
    {
        ChangeColorClientRpc(newColor, duration);
    }

    [ClientRpc]
    private void ChangeColorClientRpc(Color newColor, float duration)
    {
        if (ghostMaterial != null)
        {
            StartCoroutine(ChangeColorTemporarily(newColor, duration));
        }
    }

    private IEnumerator ChangeColorTemporarily(Color newColor, float duration) //make the material flash/light up while the ghost is paralized
    {
        ghostMaterial.color = newColor;
        yield return new WaitForSeconds(duration);
        ghostMaterial.color = originalColor;
    }

    public bool IsParalyzed() //Flashlight
    {
        return isParalyzed.Value;
    }

    public bool IsStunned() //Laser
    {
        return isStunned.Value;
    }

    #endregion

}
