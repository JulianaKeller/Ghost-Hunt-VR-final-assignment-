using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ToolAccessHandler : NetworkBehaviour
{
    #region Member Variables

    private Dictionary<int, ulong> toolOwnership = new Dictionary<int, ulong>();
    private toolsManager _toolsManager;

    #endregion

    private void Start()
    {
        _toolsManager = FindObjectOfType<toolsManager>();
    }

    #region Custom Methods

    public bool RequestAccess(int toolIndex, ulong playerId)
    {

        if (IsServer)
        {
            if (toolIndex == 0)
            {
                return true;
            }
            if (toolOwnership.ContainsKey(toolIndex))
            {
                if (toolOwnership[toolIndex] == playerId)
                    return true; // Already owns it
                return false; // Tool is in use
            }

            // Release previous tool if player had one
            foreach (var entry in toolOwnership)
            {
                if (entry.Value == playerId)
                {
                    toolOwnership.Remove(entry.Key);
                    break;
                }
            }

            toolOwnership[toolIndex] = playerId;
            return true;
        }
        return false;
    }

    public void Release(int toolIndex, ulong playerId)
    {

        if (IsServer && toolOwnership.ContainsKey(toolIndex) && toolOwnership[toolIndex] == playerId)
        {
            toolOwnership.Remove(toolIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAccessServerRpc(int toolIndex, ulong playerId, ServerRpcParams rpcParams = default)
    {
        bool granted = RequestAccess(toolIndex, playerId);

        RequestAccessClientRpc(toolIndex, granted, playerId);
    }

    [ClientRpc]
    private void RequestAccessClientRpc(int toolIndex, bool granted, ulong playerId)
    {
        if (playerId == NetworkManager.Singleton.LocalClientId)
        {
            GameObject player = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject.gameObject;

            toolsManager playerToolsManager = player.GetComponent<toolsManager>();

            if (playerToolsManager != null)
            {
                if (granted)
                    playerToolsManager.OnAccessGranted(toolIndex);
                else
                    playerToolsManager.OnAccessDenied();
            }
            else
            {
                Debug.LogError("toolsManager component not found on player object.");
            }
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
