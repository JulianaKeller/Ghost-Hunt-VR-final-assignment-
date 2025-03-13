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

    private void Awake()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            currentToolIndexNet.OnValueChanged += (oldIndex, newIndex) =>
            {
                UpdateToolClientRpc(newIndex);
            };
        }

        toolAccessHandler = GameObject.Find("ToolAccessHandler");
        if(toolAccessHandler != null){
            Debug.Log("Found ToolAccessHandler.");
        }

        currentToolIndex = 0;
        nextToolIndex = 0;

        if (toolsCollection != null)
        {
            foreach (Transform tool in toolsCollection.transform)
            {
                if (!tool.CompareTag("Hand"))
                {
                    tool.gameObject.SetActive(false);
                }
            }
        }

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
                ReactivateAllComponents(tools[index]);
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
        ReactivateAllComponents(tools[toolIndex]);

        UpdateToolClientRpc(toolIndex);
    }

    [ClientRpc]
    private void UpdateToolClientRpc(int toolIndex)
    {
        foreach (var tool in tools)
        {
            tool.SetActive(false);
        }
            

        tools[toolIndex].SetActive(true);
        ReactivateAllComponents(tools[toolIndex]);
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
        UpdateToolClientRpc(nextToolIndex);
    }

    private void ReactivateAllComponents(GameObject obj)
    {
        if (obj == null) return;

        foreach (var script in obj.GetComponents<MonoBehaviour>())
        {
            script.enabled = true;
        }

        foreach (var renderer in obj.GetComponents<Renderer>())
        {
            renderer.enabled = true;
        }

        foreach (var light in obj.GetComponents<Light>())
        {
            light.enabled = true;
        }

        foreach (var particle in obj.GetComponents<ParticleSystem>())
        {
            particle.Play();
        }

        foreach (var lineRenderer in obj.GetComponents<LineRenderer>())
        {
            lineRenderer.enabled = true;
        }

        Debug.Log($"Reactivated all components on " + obj.name);
    }
}
