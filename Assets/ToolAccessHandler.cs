using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolAccessHandler : NetworkBehaviour
{
    #region Member Variables

    private NetworkList<bool> isInUse;

    #endregion

    private void Awake()
    {
        isInUse = new NetworkList<bool>(new List<bool>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    #region Custom Methods

    public bool RequestAccess(int index)
    {
        if (index == 0)
        {
            return true;
        }
        else{
            if(isInUse[index]){
                return false;
            }
            else{
                isInUse[index] = true;
                return true;
            }
        }
    }

    //ToDo: RPC???
    public void Release(int index){
        isInUse[index] = false;
    }

    #endregion
}
