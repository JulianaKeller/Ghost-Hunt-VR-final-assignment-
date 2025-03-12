using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

public class toolsManager : NetworkBehaviour
{

    public GameObject toolsCollection;
    public InputActionProperty switchAction;

    private GameObject toolAccessHandler;

    private List<GameObject> tools = new List<GameObject>();
    private int currentToolIndex;
    private NetworkVariable<int> currentToolIndexNet = new NetworkVariable<int>(0,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private int nextToolIndex;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Debug.Log("Quitting ToolManager OnNetworkSpawn");
            return;
        }

        toolAccessHandler = GameObject.Find("ToolAccessHandler");
        if(toolAccessHandler != null){
            Debug.Log("Found ToolAccessHandler.");
        }

        currentToolIndex = 0;
        nextToolIndex = 0;

        if (toolsCollection != null)
        {
            int children = toolsCollection.transform.childCount;
            Debug.Log("Tool Count: " + children);
            for (int i = 0; i < children; ++i){
                GameObject nextTool = toolsCollection.transform.GetChild(i).gameObject;
                tools.Add(nextTool);
                if(!nextTool.CompareTag("Hand"))
                {
                    nextTool.SetActive(false);
                }
                
                Debug.Log("Tool hinzugefügt: " + toolsCollection.transform.GetChild(i).gameObject);
            }
        }
        else{
            Debug.Log("toolsCollection is Null!!!");
        }
    }

    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (switchAction.action.WasPressedThisFrame()){
            Debug.Log("Switching Tool...");
            NextTool();
        }

        if (IsClient && toolsCollection != null)
        {
            int index = currentToolIndexNet.Value;
            if (currentToolIndex != index)
            {
                tools[currentToolIndex].SetActive(false);
                tools[index].SetActive(true);
                currentToolIndex = index;
            }
        }
    }

    private void RequestToolSwitch()
    {
        if (toolAccessHandler)
        {
            toolAccessHandler.GetComponent<ToolAccessHandler>().RequestAccessServerRpc(nextToolIndex, NetworkManager.Singleton.LocalClientId);
        }
    }

    public void OnAccessGranted(int toolIndex)
    {
        if (!IsOwner) return;

        tools[currentToolIndex].SetActive(false);
        toolAccessHandler.GetComponent<ToolAccessHandler>().ReleaseServerRpc(currentToolIndex);

        currentToolIndexNet.Value = toolIndex;
        tools[toolIndex].SetActive(true);

        UpdateToolClientRpc(toolIndex);
    }

    [ClientRpc]
    private void UpdateToolClientRpc(int toolIndex)
    {
        foreach (var tool in tools)
            tool.SetActive(false);

        tools[toolIndex].SetActive(true);
    }

    public void OnAccessDenied()
    {
        NextTool();
    }

    private void NextTool()
    {
        nextToolIndex = currentToolIndex + 1;
        if (nextToolIndex >= toolsCollection.transform.childCount)
        {
            nextToolIndex = 0;
        }

        RequestToolSwitch();
    }

    //Veraltet
    /*private void SwitchTool(){
        tools[currentToolIndex].SetActive(false);
        toolAccessHandler.GetComponent<ToolAccessHandler>().Release(currentToolIndex);
        
        if(canUse()){
            Debug.Log("NextToolIndex: " + nextToolIndex);
            Debug.Log("Switching tool to " + tools[nextToolIndex]);
            tools[nextToolIndex].SetActive(true);
            currentToolIndex = nextToolIndex;
        }
        else{
            Debug.Log("Tool already in use...");
            NextTool();
        }
    }*/

    //Veraltet
    /*private bool canUse(){
        return toolAccessHandler.GetComponent<ToolAccessHandler>().RequestAccess(nextToolIndex);
    }*/
}
