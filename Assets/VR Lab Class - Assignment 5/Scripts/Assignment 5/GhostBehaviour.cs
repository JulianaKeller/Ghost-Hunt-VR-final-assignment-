using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GhostBehaviour : NetworkBehaviour
{
    private bool isParalyzed = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Rpc(SendTo.Server)]
    public void ParalyzeServerRpc(float duration)
    {
        if (!isParalyzed)
        {
            StartCoroutine(Paralyze(duration));
        }
    }

    private IEnumerator Paralyze(float duration)
    {
        isParalyzed = true;

        

        yield return new WaitForSeconds(duration);

        isParalyzed = false;
    }
}
