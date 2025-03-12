using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameTimeManager : NetworkBehaviour
{
    public Canvas canvasInGame;
    public Canvas canvasGameEnd;
    public AudioSource gameEndMusic;
    public GameObject bgSounds;
    public GameObject gameEndSounds;


    public override void OnNetworkSpawn()
    {
        // Only the server should handle the game time updates
        if (IsServer)
        {
            StartCoroutine(UpdateGameTime());
        }
    }

    private IEnumerator UpdateGameTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (IsServer)
            {
                int currentTime = NetworkVariableManager.Instance.GetGameTime();
                int gameTimeLimit = NetworkVariableManager.Instance.GetGameTimeLimit();

                currentTime += 1;

                NetworkVariableManager.Instance.SetGameTime(currentTime);

                if (currentTime >= gameTimeLimit)
                {
                    EndGame();
                    break; // Stop the coroutine after the game ends
                }
            }
        }
    }

    private void EndGame()
    {
        if (canvasInGame != null) canvasInGame.gameObject.SetActive(false);
        if (canvasGameEnd != null) canvasGameEnd.gameObject.SetActive(true);

        if(bgSounds != null)
        {
            enableSounds(bgSounds, false);
        }
        if(gameEndSounds != null)
        {
            enableSounds(gameEndSounds, true);
        }

        SpawnGhosts.Instance.DespawnAllGhostAndStopSpawning();

        if (gameEndMusic != null) gameEndMusic.Play();
    }

    private void enableSounds(GameObject sounds, bool enable)
    {
        if(sounds != null)
        {
            AudioSource[] audioSources = sounds.GetComponents<AudioSource>();

            foreach (AudioSource audioSource in audioSources)
            {
                if (enable)
                {
                    audioSource.Play();
                }
                else
                {
                    audioSource.enabled = false;
                }
            }
        }
    }
}
