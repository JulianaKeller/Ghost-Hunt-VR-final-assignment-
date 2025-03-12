using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameTimeManager : NetworkBehaviour
{
    public static GameTimeManager Instance { get; private set; } // Singleton instance

    public Canvas canvasInGame;
    public Canvas canvasGameEnd;
    public AudioSource gameEndMusic;
    public GameObject bgSounds;
    public GameObject gameEndSounds;
    public SkyboxController skyboxController;
    public moonMovement moonMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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

    public IEnumerator RestartGame()
    {
        NetworkVariableManager.Instance.SetCaughtGhostsCount(0);
        NetworkVariableManager.Instance.SetGameTime(0);

        skyboxController.ResetExposure();
        moonMovement.ResetMoonPosition();

        if (bgSounds != null)
        {
            enableSounds(bgSounds, true);
        }
        if (gameEndSounds != null)
        {
            enableSounds(gameEndSounds, false);
        }

        if (canvasInGame != null) canvasInGame.gameObject.SetActive(true);
        if (canvasGameEnd != null) canvasGameEnd.gameObject.SetActive(false);

        yield return new WaitForSeconds(5f);

        SpawnGhosts.Instance.EnableSpawning();
    }

    private void enableSounds(GameObject sounds, bool enable, float duration = 1.5f)
    {
        //ToDo enable or disable slowly (decrease or increase volume)
        if(sounds != null)
        {
            AudioSource[] audioSources = sounds.GetComponents<AudioSource>();

            foreach (AudioSource audioSource in audioSources)
            {
                StartCoroutine(FadeAudio(audioSource, enable, duration));
            }
        }
    }

    private IEnumerator FadeAudio(AudioSource audioSource, bool enable, float duration)
    {
        float startVolume = audioSource.volume;
        float targetVolume = enable ? 1f : 0f;
        float elapsedTime = 0f;

        if (enable)
        {
            audioSource.Play();
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (!enable)
        {
            audioSource.Stop();
        }
    }
}
