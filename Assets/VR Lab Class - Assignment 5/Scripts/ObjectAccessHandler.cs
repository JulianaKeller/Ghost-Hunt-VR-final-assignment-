using Unity.Netcode;

public class ObjectAccessHandler : NetworkBehaviour
{
    #region Member Variables

    private NetworkVariable<bool> isInUse = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #endregion

    #region Custom Methods

    public bool RequestAccess()
    {
        if (IsServer) return false; // Prevent server from making the request because LocalClientId only applies to clients

        if (gameObject.tag == "Hand"){
            return true;
        }
        //check if the object is already grabbed, if yes return false
        if (isInUse.Value)
        {
            return false;
        }

        else
        {
            //trigger the change of ownership to the client sending the RPC
            //trigger the update of the isGrabbed NetworkVariable. Only the server can change this variable
            UpdateOwnershipIsGrabbedRpc(NetworkManager.LocalClientId);
            return true;
        }
    }

    public void Release()
    {
        //Check if the object is grabbed and owned by the local client
        if(isInUse.Value == true && IsOwner)
        {
            UpdateReleaseRpc();
        }
    }

    #endregion

    #region RPCs

    //SendTo.Server because only the server can change the isGrabbed NetworkVariable and ownership
    [Rpc(SendTo.Server)]
    private void UpdateOwnershipIsGrabbedRpc(ulong clientID)
    {
        //Only the server can change isGrabbed and ownership
        if (IsServer)
        {
            NetworkObject.ChangeOwnership(clientID);
            isInUse.Value = true;
        }
    }

    //SendTo.Server because only the server can change the isGrabbed NetworkVariable and ownership
    [Rpc(SendTo.Server)]
    private void UpdateReleaseRpc()
    {
        isInUse.Value = false;

        //Reset ownerhip
        NetworkObject.RemoveOwnership();
    }

    #endregion
}