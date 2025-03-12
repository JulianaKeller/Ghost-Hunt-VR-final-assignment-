using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnGhosts : NetworkBehaviour
{
    public static SpawnGhosts Instance { get; private set; } // Singleton Instance

    [Header("For Testing")]
    public bool isAlwaysParalyzed = false;
    public bool isAlwaysStunned = false;

    [Header("References")]

    public GameObject ghostPrefab;
    public GameObject spawnPointsParent;
    [Range(1, 10)]

    [Header("Settings")]

    private int minGhostCount = 3;
    public float spawnInterval = 5f;
    private bool spawningEnabled = true;
    private Coroutine spawnRoutine;

    private List<GameObject> activeGhosts = new List<GameObject>();
    private Transform[] spawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn called. IsServer: " + IsServer);

        if (!IsServer) return; // Only the server handles spawning

        minGhostCount = NetworkVariableManager.Instance.GetDifficultyProperties().SpawnGhostsCount;

        if (spawnPointsParent != null)
        {
            spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
            if (spawnPoints.Length > 1) spawnPoints = spawnPoints[1..]; //Filter out parent transform
        }

        if (IsServer) // Ensure this only runs on the server
        {
            Debug.Log("IsServer and starting SpawnRoutine now...");
            spawnRoutine = StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.Log("Is not server, can't start SpawnRoutine!");
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true && spawningEnabled)
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
            RequestDespawnGhost(activeGhosts[activeGhosts.Count - 1]);
        }
    }

    public void RequestDespawnGhost(GameObject obj)
    {
        if (IsServer)
        {
            DespawnGhost(obj); // If called on server, just despawn immediately
        }
        else
        {
            DespawnGhostServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId); // Clients must send a request
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnGhostServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            DespawnGhost(netObj.gameObject);
        }
    }

    private void DespawnGhost(GameObject obj)
    {
        if (IsServer)
        {
            NetworkObject netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn(true);
                activeGhosts.Remove(obj);
            }
        }
    }

    public void DespawnAllGhostAndStopSpawning()
    {
        if (IsServer)
        {
            // Create a copy of the list to avoid modifying it while iterating
            List<GameObject> ghostsToDespawn = new List<GameObject>(activeGhosts);

            foreach (GameObject ghost in ghostsToDespawn)
            {
                NetworkObject netObj = ghost.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }

            activeGhosts.Clear(); // Clear the original list after iteration
            spawningEnabled = false;
        }
    }

    void SpawnGhost()
    {
        if (spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            GameObject newGhost = Instantiate(ghostPrefab, new Vector3(spawnPoint.position.x, -2.5f, spawnPoint.position.z), randomRotation);
            newGhost.transform.GetComponent<GeistBewegung>().SetTestingMode(isAlwaysParalyzed, isAlwaysStunned);

            NetworkObject instanceNetworkObject = newGhost.GetComponent<NetworkObject>();
            if (instanceNetworkObject != null && IsServer) // Ensure this is running only on the server
            {
                instanceNetworkObject.Spawn();
                activeGhosts.Add(newGhost);
            }
        }
    }

    public void EnableSpawning()
    {
        if (!spawningEnabled)
        {
            if (IsServer)
            {
                spawningEnabled = true;
                if (spawnRoutine == null)
                {
                    spawnRoutine = StartCoroutine(SpawnRoutine());
                }
            }
        }
    }
}
