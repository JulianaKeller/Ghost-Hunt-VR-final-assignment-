using UnityEngine;
using UnityEngine.InputSystem;
using VRSYS.Core.Avatar;

public class TeleportNavigation : MonoBehaviour
{
    public InputActionReference teleportAction;

    public Transform head;
    public Transform hand;

    public LayerMask groundLayerMask;

    public GameObject previewAvatar;
    public GameObject hitpoint;
    
    public float rayLength = 10.0f;
    private bool rayIsActive = false;

    public LineRenderer lineVisual;
    private float rayActivationThreshhold = 0.01f;
    private float teleportActivationThreshhold = 0.9f;
    
    private bool previewIsActive = false;
    private Vector3 currentHitPoint;
    private Vector3 targetPoint;
    private Quaternion targetRotationAvatar = Quaternion.identity;
    private Quaternion targetRotationPlayer = Quaternion.identity;

    //private Vector3 initialPlayerPosition;

    // Start is called before the first frame update
    void Start()
    {
        if (head == null)
            head = GetComponent<AvatarHMDAnatomy>().head;

        if (hand == null)
            hand = GetComponent<AvatarHMDAnatomy>().rightHand;
        
        lineVisual.positionCount = 2; // line between two vertices
        lineVisual.enabled = false;
        hitpoint.SetActive(false);
        previewAvatar.SetActive(false);
    }

    void Update()
    {
        float teleportActionValue = teleportAction.action.ReadValue<float>();
        
        if(!previewAvatar.activeSelf){
            setPreviewAvatar(teleportActionValue);
        }
        else{
            rotatePreviewAvatar(teleportActionValue);
        }
    }

    private void setPreviewAvatar(float teleportActionValue){
        if (teleportActionValue > rayActivationThreshhold)
        {
            Vector3 rayStart = hand.position;
            Vector3 rayDirection = hand.forward;

            // Perform the raycast
            if (Physics.Raycast(hand.position, hand.forward, out RaycastHit hit, rayLength+100, groundLayerMask))
            {
                // Show hitpoint at the intersection
                currentHitPoint = hit.point;
                hitpoint.SetActive(true);
                hitpoint.transform.position = currentHitPoint;

                //Debug.Log("Raycast hit at: " + currentHitPoint);

                // Update LineRenderer
                ExampleUpdateLineVisual(true, rayStart, hit.point, Color.green);

                 // Handle teleport activation threshold
                if (teleportActionValue > teleportActivationThreshhold)
                {
                    // Set target location and show preview avatar
                    targetPoint = currentHitPoint;
                    previewAvatar.SetActive(true);
                    previewAvatar.transform.position = targetPoint;

                    //Höhe setzten - ToDo
                    if(Physics.Raycast(head.position, Vector3.down, out RaycastHit headDistanceHit, Mathf.Infinity, groundLayerMask)){
                        float currentHeadToGroundDistance = headDistanceHit.distance;
                        float offset = currentHitPoint.y + currentHeadToGroundDistance;
                        previewAvatar.transform.position = new Vector3(previewAvatar.transform.position.x, offset, previewAvatar.transform.position.z);
                    }
                    else{
                        //Debug.Log("No ground detected.");
                    }
                }
            }
            else
            {
                // No hit; deactivate indicators
                hitpoint.SetActive(false);
                ExampleUpdateLineVisual(false, Vector3.zero, Vector3.zero, Color.clear);
                // Hide preview avatar if there's no valid hitpoint
                previewAvatar.SetActive(false);
            }
        }
        else
        {
            // Deactivate everything if the trigger is below threshold
            lineVisual.enabled = false;
            hitpoint.SetActive(false);
            previewAvatar.SetActive(false);

        }
    }

    private void rotatePreviewAvatar(float teleportActionValue){
        
        if(teleportActionValue > teleportActivationThreshhold){
            if (Physics.Raycast(hand.position, hand.forward, out RaycastHit hit, rayLength+100, groundLayerMask)){ //ToDo Layer...
                // Update LineRenderer
                ExampleUpdateLineVisual(true, hand.position, hit.point, Color.green);

                Vector3 rotationTargetPoint = hit.point;

                // Calculate the direction vector from the avatar to the target point
                Vector3 directionToTarget = rotationTargetPoint - previewAvatar.transform.position;
                directionToTarget.y = 0; // Only rotate around y axis

                // Set the avatar's rotation to face the current targetPoint
                if (directionToTarget.sqrMagnitude > 0.01f)
                {
                    targetRotationAvatar = Quaternion.LookRotation(-directionToTarget);
                    targetRotationPlayer = Quaternion.LookRotation(directionToTarget);
                    previewAvatar.transform.rotation = targetRotationAvatar;
                }
            }   
        }
        else{
            //Teleport

            //Vector3 offset = head.position - transform.position;
            //offset.y = 0;
            
            transform.position = targetPoint;

            transform.rotation = targetRotationPlayer;
            head.rotation = targetRotationPlayer;

            hitpoint.SetActive(false);
            previewAvatar.SetActive(false);
        }
        
    }

    private void ExampleUpdateLineVisual(bool rayActive, Vector3 startPosition, Vector3 endPosition, Color color)
    {
        lineVisual.enabled = rayActive;
        lineVisual.SetPosition(0, startPosition);
        lineVisual.SetPosition(1, endPosition);
        lineVisual.startColor = color;
        lineVisual.endColor = color;
    }
    
    
}