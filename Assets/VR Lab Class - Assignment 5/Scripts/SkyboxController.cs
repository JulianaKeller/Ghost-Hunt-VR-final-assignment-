using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    public float startExposure = 0.2f;
    public float endExposure = 5f;

    private Material skyboxMat;
    // Start is called before the first frame update
    void Start()
    {
        skyboxMat = RenderSettings.skybox;
    }

    // Update is called once per frame
    void Update()
    {
        float exposure = startExposure;

        int currentTime = NetworkVariableManager.Instance.GetGameTime();
        int gameTimeLimit = NetworkVariableManager.Instance.GetGameTimeLimit();

        exposure = Exposure(currentTime);

        //Debug.Log("Exposure: " + exposure);

        //Apply exposure to skybox
        skyboxMat.SetFloat("_Exposure", exposure);
    }

    private float Exposure(int t)
    {
        //Exponentialfunktion mit y=0.2 bzw startExposure für x=0 und y=5 bzw endExposure für x=timeLimit:
        //exposure(t) = a*b^t mit t = time
        //mit a = startExposure
        //log(b) = log(endExposure/startExposure)/timeLimit <=> b = 10^(log(endExposure/startExposure)/timeLimit)
        float a = startExposure;
        float exponent = Mathf.Log(endExposure / startExposure, 10)/ NetworkVariableManager.Instance.GetGameTimeLimit();
        float b = Mathf.Pow(10, exponent);

        //Debug.Log("New exposure value: " + (a * Mathf.Pow(b, t)));

        return (a * Mathf.Pow(b, t));
    }
}
