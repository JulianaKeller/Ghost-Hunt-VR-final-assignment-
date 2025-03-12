using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualHand : NetworkBehaviour
{
    #region Member Variables

    public InputActionProperty grabAction;
    public HandCollider handCollider;
    public Rigidbody handRigidbody;

    private GameObject grabbedObject;
    private Rigidbody grabbedRb;
    private Matrix4x4 offsetMatrix;

    //Velocity calculations
    public Transform controller;
    private Vector3 previousPosition;
    private Vector3 handVelocity;
    private Vector3 angularVelocity;
    private float velocityThreshold = 0.01f;
    private Vector3 smoothedVelocity;
    private float smoothingFactor = 0.1f;

    private bool canGrab //can only grab when it is the player's own tool, can only grab the captureball
    {
        get
        {
            if (handCollider.isColliding)
            {
                GameObject obj = handCollider.collidingObject;
                if (obj.CompareTag("CaptureBall") && obj.GetComponent<NetworkObject>().IsOwner)
                {
                    Debug.Log("Can grab " + obj);
                    return true;
                }
                else
                {
                    Debug.Log("Can't grab " + obj);
                }
            }
            return false;
        }
    }

    #endregion

    #region MonoBehaviour Callbacks

    public override void OnNetworkSpawn()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
        {
            Destroy(this);
            return;
        }
    }

    private void Update()
    {
        calculateHandVelocity();
        HandleGrab();
    }

    #endregion

    #region Custom Methods

    private void calculateHandVelocity()
    {
        handVelocity = (controller.position - previousPosition) / Time.deltaTime;
        previousPosition = controller.position;

        if (handVelocity.magnitude < velocityThreshold)
        {
            handVelocity = Vector3.zero;  // Don't apply velocity if too small
        }

        // Smooth the velocity
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, handVelocity, smoothingFactor);
    }

    private void HandleGrab()
    {
        if (grabAction.action.WasPressedThisFrame())
        {
            Debug.Log("GrabAction was pressed...");
            if (grabbedObject == null && canGrab)
            {
                grabbedObject = handCollider.collidingObject;
                grabbedRb = grabbedObject.GetComponent<Rigidbody>();
                Debug.Log("Grabbed " + grabbedObject);

                if (grabbedObject.TryGetComponent(out NetworkObject netObj))
                {
                    if (!netObj.IsOwner)
                    {
                        netObj.ChangeOwnership(NetworkManager.Singleton.LocalClientId);
                    }
                }

                // Attempt to get the follow script component.
                CaptureBallFollowMode followScript = grabbedObject.GetComponent<CaptureBallFollowMode>();
                if (followScript != null)
                {
                    // This disables the follow behavior so the ball stops tracking the spawn point.
                    followScript.DetachBall();
                }

                if (grabbedRb != null)
                {
                    grabbedRb.isKinematic = true; // Disable physics while held
                    grabbedRb.useGravity = false;
                    Debug.Log("Set Rigidbody to kinematic without gravity");
                }
                else
                {
                    Debug.Log("No Rigidbody found!");
                    grabbedObject = null;
                    return;
                }
                offsetMatrix = GetTransformationMatrix(transform, true).inverse *
                               GetTransformationMatrix(grabbedObject.transform, true);
            }
        }
        else if (grabAction.action.IsPressed())
        {
            if (grabbedObject != null)
            {
                Matrix4x4 newTransform = GetTransformationMatrix(transform, true) * offsetMatrix;

                grabbedObject.transform.position = newTransform.GetColumn(3);
                grabbedObject.transform.rotation = newTransform.rotation;

            }
        }
        else if (grabAction.action.WasReleasedThisFrame()) //inherit velocity here
        {
            

            Debug.Log("GrabbedAction was released...");
            if(grabbedObject != null)
            {
                if (grabbedRb != null)
                {
                    grabbedRb.isKinematic = false; // Enable physics again
                    grabbedRb.useGravity = true;

                    if (IsOwner) // Only the server should apply physics updates
                    {
                        // Apply velocity locally first to avoid physics issues
                        grabbedRb.velocity = 2 * smoothedVelocity;
                        //grabbedRb.angularVelocity = handRigidbody.angularVelocity;

                        Debug.Log("Hands Velocity: " + handVelocity);
                        Debug.Log("Objects Velocity: " + grabbedRb.velocity);

                        grabbedObject.GetComponent<CaptureBallLogic>().Throw();
                    }
                }
            }
            else
            {
                Debug.Log("GrabbedObject is null!");
            }
            grabbedObject = null;
            grabbedRb = null;
            offsetMatrix = Matrix4x4.identity;
        }
    }

    #endregion
    
    #region Utility Functions

    public Matrix4x4 GetTransformationMatrix(Transform t, bool inWorldSpace = true)
    {
        if (inWorldSpace)
        {
            return Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
        }
        else
        {
            return Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
        }
    }

    #endregion

    /*#region Server Networking

    [ServerRpc(RequireOwnership = false)]
    private void ThrowObjectServerRpc(NetworkObjectReference objectRef, Vector3 velocity, Vector3 angularVelocity)
    {
        if (objectRef.TryGet(out NetworkObject obj))
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // Ensure kinematic is disabled before applying physics
                rb.useGravity = true;
                //rb.velocity = velocity;
                //rb.angularVelocity = angularVelocity;
            }
        }
    }

    #endregion*/
}
