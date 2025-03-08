using Unity.Netcode;
using UnityEngine;

public class HandCollider : MonoBehaviour
{
    #region Member Variables

    public bool isColliding = false;
    public GameObject collidingObject = null;

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

    private void OnTriggerEnter(Collider other)
    {
        if (!isColliding)
        {
            Debug.Log("Hand Collided with " + other.gameObject);
            collidingObject = other.gameObject;
            isColliding = true;

            // Check if hand hit a difficulty button
            DifficultyButton button = other.gameObject.GetComponent<DifficultyButton>();
            if (button != null)
            {
                button.SetDifficulty();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isColliding && other.gameObject == collidingObject)
        {
            Debug.Log("Hand Stopped collidiing with " + other.gameObject);
            collidingObject = null;
            isColliding = false;
        }
    }

    #endregion
}
