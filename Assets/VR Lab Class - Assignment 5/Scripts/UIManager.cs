using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI ghostCountText;


    // Start is called before the first frame update
    void Start()
    {
        NetworkVariableManager.Instance.GameTime.OnValueChanged += updateTimeText;
        NetworkVariableManager.Instance.CaughtGhostsCount.OnValueChanged += updateGhostCount;

        updateTimeText(0, 0);
        updateGhostCount(0, 0);
    }

    // Update is called once per frame
    private void updateTimeText(int oldValue, int newValue)
    {
        if(timeText != null)
        {
            int currentTime = newValue;
            int gameTimeLimit = NetworkVariableManager.Instance.GetGameTimeLimit();
            int timeLeft = gameTimeLimit - currentTime;

            timeText.text = timeLeft.ToString();
        }
    }

    private void updateGhostCount(int oldValue, int newValue)
    {
        if (ghostCountText != null)
        {
            ghostCountText.text = newValue.ToString();
        }
    }

    private void OnDestroy()
    {
        //unsubscribe to prevent memory leaks

        NetworkVariableManager.Instance.GameTime.OnValueChanged -= updateTimeText;
        NetworkVariableManager.Instance.CaughtGhostsCount.OnValueChanged -= updateGhostCount;
    }
}
