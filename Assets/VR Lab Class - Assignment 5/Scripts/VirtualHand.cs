using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualHand : MonoBehaviour
{
    
    #region Member Variables

    public InputActionProperty grabAction;
    public HandCollider handCollider;
    public Rigidbody handRigidbody;

    private GameObject grabbedObject;
    private Rigidbody grabbedRb;
    private Matrix4x4 offsetMatrix;

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

    private void Start()
    {
        if (!GetComponentInParent<NetworkObject>().IsOwner)
        {
            Destroy(this);
            return;
        }
    }

    private void Update()
    {
        HandleGrab();
    }

    #endregion

    #region Custom Methods

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
        else if (grabAction.action.WasReleasedThisFrame()) //inherit velocity here?
        {
            Debug.Log("GrabbedAction was released...");
            if(grabbedObject != null)
            {
                grabbedRb.isKinematic = false; // Enable physics again
                grabbedRb.useGravity = true;
                grabbedRb.velocity = handRigidbody.velocity; // Inherit velocity
                grabbedRb.angularVelocity = handRigidbody.angularVelocity;
                Debug.Log("Reenabled physics for " + grabbedObject);
                Debug.Log("Objects Velocity: " + grabbedRb.velocity);

                grabbedObject.GetComponent<CaptureBallLogic>().Throw();
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
}
