using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class DifficultyButton : MonoBehaviour
{
    public int difficultyLevel;
    public float pressDepth = 0.02f;
    public float pressDuration = 0.5f;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            Debug.Log("Button Pressed!");
            SetDifficulty();
        }
    }

    public void SetDifficulty()
    {
        NetworkVariableManager.Instance.SetGameDifficulty(difficultyLevel);
        StartCoroutine(PressAnimation());
    }

    private IEnumerator PressAnimation()
    {
        Vector3 pressedPosition = originalPosition - new Vector3(pressDepth, 0, 0);

        // Move down
        float elapsedTime = 0f;
        while (elapsedTime < pressDuration / 2)
        {
            transform.position = Vector3.Lerp(originalPosition, pressedPosition, elapsedTime / (pressDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = pressedPosition;

        yield return new WaitForSeconds(0.2f);

        // Move up
        elapsedTime = 0f;
        while (elapsedTime < pressDuration / 2)
        {
            transform.position = Vector3.Lerp(pressedPosition, originalPosition, elapsedTime / (pressDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }
}
