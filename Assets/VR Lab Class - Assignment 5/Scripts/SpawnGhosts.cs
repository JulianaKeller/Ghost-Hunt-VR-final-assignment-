using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnGhosts : MonoBehaviour
{
    public GameObject ghostPrefab;
    public GameObject spawnPointsParent; // Mögliche Spawnpunkte
    [Range(1, 10)]
    public int minGhostCount = 3; // Mindestanzahl an Geistern in der Szene
    public float spawnInterval = 5f; // Zeitintervall für Überprüfung/Spawn

    private List<GameObject> activeGhosts = new List<GameObject>();
    private Transform[] spawnPoints;

    // Start is called before the first frame update
    void Start()
    {
        if (spawnPointsParent != null)
        {
            spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
        }
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            CheckAndSpawnGhosts();
        }
    }

    void CheckAndSpawnGhosts()
    {
        activeGhosts.RemoveAll(ghost => ghost == null); // Entferne zerstörte Geister
        
        if (activeGhosts.Count < minGhostCount)
        {
            SpawnGhost();
        }
    }

    void SpawnGhost()
    {
        if (spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            GameObject newGhost = Instantiate(ghostPrefab, spawnPoint.position, randomRotation);
            activeGhosts.Add(newGhost);
        }
    }
}
