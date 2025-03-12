using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GhostSounds : NetworkBehaviour
{
    [Header("Audio Settings")]
    public AudioSource ghostSoundAudioSource;
    public List<AudioClip> ghostSoundClips;
    public float maxVolume = 1.0f;
    public float minVolume = 0.0f;
    public float minPauseDuration = 1.0f;
    public float maxPauseDuration = 5.0f;

    private Transform vrPlayerTransform;

    void Start()
    {
        if (ghostSoundAudioSource != null && ghostSoundClips.Count > 0)
        {
            ghostSoundAudioSource.loop = false; // Make sure to not loop the audio
            StartCoroutine(PlayRandomGhostSound());
        }

        if (IsOwner)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                NetworkObject playerNetworkObject = playerObject.GetComponent<NetworkObject>();

                // Ensure that this player is the local player
                if (playerNetworkObject != null && playerNetworkObject.IsOwner)
                {
                    vrPlayerTransform = playerObject.transform;
                }
            }
            else
            {
                Debug.LogError("Player object not found!");
            }
        }
    }

    void Update()
    {
        // Update the volume of the ghost's sound based on the distance to the player
        UpdateGhostSoundVolume();
    }

    private IEnumerator PlayRandomGhostSound()
    {
        while (true)
        {
            // Pick a random clip from the list
            AudioClip randomClip = ghostSoundClips[Random.Range(0, ghostSoundClips.Count)];

            // Set the new clip and play it
            ghostSoundAudioSource.clip = randomClip;
            ghostSoundAudioSource.Play();

            // Wait for the duration of the clip before doing the next one
            yield return new WaitForSeconds(randomClip.length);

            // Wait for a random pause before playing the next clip
            float randomPause = Random.Range(minPauseDuration, maxPauseDuration);
            yield return new WaitForSeconds(randomPause);
        }
    }

    void UpdateGhostSoundVolume()
    {
        if (vrPlayerTransform != null && ghostSoundAudioSource != null)
        {
            // Calculate distance between VR player and ghost
            float distanceToPlayer = Vector3.Distance(transform.position, vrPlayerTransform.position);

            // Adjust the volume based on distance
            float volume = Mathf.Clamp01(1 / distanceToPlayer); // Inversely proportional to the distance
            ghostSoundAudioSource.volume = Mathf.Lerp(minVolume, maxVolume, volume);
        }
    }
}
