using Unity.Netcode;
using UnityEngine;

public class RayHitpointSerializer : NetworkBehaviour
{
    public GameObject hitpoint;
    public LineRenderer ray;
    public Transform hand;
    private NetworkVariable<bool> rayHitpointEnabled = new NetworkVariable<bool>(default, writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> hitpointPosition = new NetworkVariable<Vector3>(default, writePerm: NetworkVariableWritePermission.Owner);
    
    public override void OnNetworkSpawn()
    {
        hitpoint.SetActive(false);
        ray.positionCount = 2;
        ray.enabled = false;

        if (IsOwner)
            return;

        ApplyRayUpdates();
    }
    
    private void Update()
    {
        if (IsOwner)
        {
            SerializeRayUpdates();
        }
        else
        {
            ApplyRayUpdates();
        }
    }
    
    private void SerializeRayUpdates()
    {
        rayHitpointEnabled.Value = hitpoint.activeSelf;
        hitpointPosition.Value = hitpoint.transform.position;
    }

    private void ApplyRayUpdates()
    {
        hitpoint.SetActive(rayHitpointEnabled.Value);
        ray.enabled = rayHitpointEnabled.Value;
        if (hitpoint.activeSelf)
        {
            ray.SetPosition(0, hand.position);
            ray.SetPosition(1, hitpointPosition.Value);
            hitpoint.transform.position = hitpointPosition.Value;
        }
    }

}
