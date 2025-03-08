using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ToolAccessHandler : NetworkBehaviour
{
    #region Member Variables

    //public GameObject toolsCollection;

    //private NetworkList<bool> isInUse;
    private Dictionary<int, ulong> toolOwnership = new Dictionary<int, ulong>();
    private toolsManager _toolsManager;

    #endregion

    /*private void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); // Always call the base method first

        isInUse = new NetworkList<bool>(new List<bool>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


        //Fill the list with true values for each tool:
        if (IsServer)
        {
            if (toolsCollection != null)
            {
                int children = toolsCollection.transform.childCount;
                for (int i = 0; i < children; ++i)
                {
                    isInUse.Add(true);
                }
            }
            else
            {
                Debug.Log("toolsCollection is Null!");
            }
        }
    }*/

    private void Start()
    {
        _toolsManager = FindObjectOfType<toolsManager>();
    }

    #region Custom Methods

    public bool RequestAccess(int toolIndex, ulong playerId)
    {
        /*if (!IsServer) return false; // Only server can modify NetworkList
        if (index == 0)
        {
            return true;
        }
        else if (index >= isInUse.Count || index < 0)
        {
            return false;
        }
        else {
            if(isInUse[index]){
                return false;
            }
            else{
                isInUse[index] = true;
                return true;
            }
        }*/

        if (IsServer)
        {
            if (toolIndex == 0)
            {
                return true;
            }
            if (toolOwnership.ContainsKey(toolIndex))
            {
                return false; // Tool is in use
            }

            toolOwnership[toolIndex] = playerId;
            return true;
        }
        return false;
    }

    public void Release(int toolIndex, ulong playerId)
    {
        /*if (!IsServer) return; // Only server can modify NetworkList
        isInUse[index] = false;*/

        if (IsServer && toolOwnership.ContainsKey(toolIndex) && toolOwnership[toolIndex] == playerId)
        {
            toolOwnership.Remove(toolIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAccessServerRpc(int toolIndex, ServerRpcParams rpcParams = default)
    {
        ulong playerId = rpcParams.Receive.SenderClientId;
        bool granted = RequestAccess(toolIndex, playerId);

        RequestAccessClientRpc(toolIndex, granted, playerId);
    }

    [ClientRpc]
    private void RequestAccessClientRpc(int toolIndex, bool granted, ulong playerId)
    {
        if (playerId == NetworkManager.Singleton.LocalClientId)
        {
            if (granted)
                _toolsManager.OnAccessGranted(toolIndex);
            else
                _toolsManager.OnAccessDenied();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseServerRpc(int toolIndex, ServerRpcParams rpcParams = default)
    {
        Release(toolIndex, rpcParams.Receive.SenderClientId);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        List<int> toRemove = new List<int>();

        foreach (var kvp in toolOwnership)
        {
            if (kvp.Value == clientId)
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            toolOwnership.Remove(key);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    #endregion
}
