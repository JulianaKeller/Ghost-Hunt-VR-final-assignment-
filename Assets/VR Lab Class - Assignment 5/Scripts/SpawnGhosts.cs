using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnGhosts : NetworkBehaviour
{
    public GameObject ghostPrefab;
    public GameObject spawnPointsParent; // Mögliche Spawnpunkte
    [Range(1, 10)]
    private int minGhostCount = 3; // Mindestanzahl an Geistern in der Szene
    public float spawnInterval = 5f; // Zeitintervall für Überprüfung/Spawn

    private List<GameObject> activeGhosts = new List<GameObject>();
    private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn called. IsServer: " + IsServer);

        minGhostCount = NetworkVariableManager.Instance.GetDifficultyProperties().SpawnGhostsCount;

        if (spawnPointsParent != null)
        {
            spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
            if (spawnPoints.Length > 1) spawnPoints = spawnPoints[1..]; //Filter out parent transform
        }

        if (IsServer) // Ensure this only runs on the server
        {
            Debug.Log("IsServer and starting SpawnRoutine now...");
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.Log("Is not server, can't start SpawnRoutine!");
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (IsServer) // Ensure only the server spawns
            {
                CheckAndSpawnGhosts();
            }
        }
    }

    void CheckAndSpawnGhosts()
    {
        minGhostCount = NetworkVariableManager.Instance.GetDifficultyProperties().SpawnGhostsCount;

        activeGhosts.RemoveAll(ghost => ghost == null); // Entferne zerstörte Geister
        
        if (activeGhosts.Count < minGhostCount)
        {
            SpawnGhost();
        }
        else if(activeGhosts.Count > minGhostCount)
        {
            DespawnGhost(activeGhosts[activeGhosts.Count - 1]);
        }
    }

    public void DespawnGhost(GameObject obj)
    {
        if (IsServer)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj.IsSpawned && netObj.IsOwner)
            {
                netObj.Despawn(true);
                activeGhosts.Remove(obj);
            }
        }
    }

    void SpawnGhost()
    {
        if (spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject newGhost = Instantiate(ghostPrefab, new Vector3(spawnPoint.position.x, -2.5f, spawnPoint.position.z), randomRotation);

            NetworkObject instanceNetworkObject = newGhost.GetComponent<NetworkObject>();
            if (instanceNetworkObject != null && IsServer) // Ensure this is running only on the server
            {
                instanceNetworkObject.Spawn();
                activeGhosts.Add(newGhost);
            }
        }
    }
}
