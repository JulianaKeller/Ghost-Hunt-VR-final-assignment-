using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using VRSYS.Core.Avatar;
using VRSYS.Core.Networking;

public class ThumbstickNavigation : MonoBehaviour
{
    public enum InputMapping
    {
        PositionControl,
        VelocityControl,
        AccelerationControl
    }

    [Header("General Configuration")]
    public InputMapping inputMapping = InputMapping.PositionControl;
    public Transform head;
    public InputActionReference steeringAction;
    public InputActionReference rotationAction;
    
    [Header("Navigation Configuration")]
    private Vector3 startingPosition;

    public float movementSpeed = 3f;

    private float accelerationFactor = 1f;
    public float accelerationFactorControl = 1f;

    private float accelerationStart;
    public float positionOffset = 10f; // max position offset during position control 
    [Range(0.1f, 30.0f)] public float steeringSpeed = 10f; // max steering speed for rate control
    [Range(0.1f, 100.0f)] public float maxAcceleration = 2f; // max acceleration during acceleration control
    [Range(0.1f, 100.0f)] public float maxVelocity = 5; // max velocity reached during acceleration control
    private Vector3 currentVelocity = Vector3.zero;
    
    [Header("Rotation Configuration")]
    [Range(1.0f, 180.0f)] // Draws a slider with range in the inspector 
    public float rotationSpeed = 3f; // In angle degrees per second
    public bool snapRotation = true;
    public float snapAngles = 30f; // In angle degrees per snap
    private bool hasMoved = false;
    private bool accelerationStarted = false;
    private bool boundaryPassed = false;

    [Header("Groundfollowing Configuration")]
    public LayerMask groundLayerMask;
    private RaycastHit hit;

    [Header("Boundary Configuration")]
    public LayerMask boundaryLayerMask;
    public float boundaryCheckDistance = 0.2f;

    private void Start()
    {
        if (head == null)
            head = GetComponent<AvatarHMDAnatomy>().head;
        
        // Reference point for computing position control
        startingPosition = transform.position;
    }

    void Update()
    {
        ApplyDisplacement();
        ApplyRotation();
        ApplyGroundfollowing();
    }

    private void ApplyDisplacement()
    {
        Vector2 input = steeringAction.action.ReadValue<Vector2>();
        VelocityControl(input);
    }
    
    private void VelocityControl(Vector2 input)
    {
        Vector2 velocityInput = steeringAction.action.ReadValue<Vector2>();

        if(velocityInput.sqrMagnitude > 0.01f){
            velocityInput = velocityInput * movementSpeed;
            if(velocityInput.sqrMagnitude > steeringSpeed*steeringSpeed){
                velocityInput = velocityInput.normalized * steeringSpeed;
            }

            //Vector3 displacement = new Vector3(velocityInput.x, 0f, velocityInput.y) * Time.deltaTime;

            Vector3 movementDirection = head.forward.normalized * velocityInput.y + head.right.normalized * velocityInput.x;
            movementDirection.y = 0;
            movementDirection = -movementDirection;
            Vector3 displacement = movementDirection * Time.deltaTime;

            if (!IsBoundaryAhead(movementDirection))
            {
                transform.Translate(displacement, Space.World);
            }
        }
    }

    private bool IsBoundaryAhead(Vector3 direction)
    {
        return Physics.Raycast(transform.position, direction.normalized, boundaryCheckDistance, boundaryLayerMask);
    }

    private void ApplyRotation()
    {
        Vector2 rotationInput = rotationAction.action.ReadValue<Vector2>();

        float rotationAmount = 0;
        //Check for input
        if(rotationInput.sqrMagnitude > 0.01f){
            //Calculate target rotation
            if(snapRotation){
                if(!hasMoved){
                    Debug.Log("snaping.....");
                    rotationAmount = (Mathf.Sign(rotationInput.x) * snapAngles);
                    hasMoved = true;
                }
            }
            else{
                Debug.Log("rotating smoothly...........");
                rotationAmount = (rotationInput.x * rotationSpeed * Time.deltaTime);
            }
            transform.Rotate(0f, rotationAmount, 0f, Space.World);
        }
        else if(hasMoved){
            Debug.Log("hasMoved ist wieder auf false.....");
            hasMoved = false;
        }
    }
    
    private void ApplyGroundfollowing()
    {
        float realWorldHeadFloorDistance = 1.6f;

        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        XRNodeState centerEyeNode = nodeStates.Find(nameof => nameof.nodeType == XRNode.CenterEye);

        if(centerEyeNode.TryGetPosition(out Vector3 headPosition)){
            realWorldHeadFloorDistance = headPosition.y;
            //Debug.Log("Head height from floor: " + realWorldHeadFloorDistance);
        }

        if(Physics.Raycast(head.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayerMask)){
            float currentHeadToGroundDistance = hit.distance;
            float offset = realWorldHeadFloorDistance - currentHeadToGroundDistance;
            transform.position = new Vector3(transform.position.x, transform.position.y + offset, transform.position.z);
        }
        else{
            //Debug.Log("No ground detected.");
        }
    }
}