using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVariableManager : NetworkBehaviour
{
    public static NetworkVariableManager Instance;

    // Game-wide variables
    public NetworkVariable<int> CaughtGhostsCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> GameTime = new NetworkVariable<int>(0);
    public NetworkVariable<int> GameTimeLimit = new NetworkVariable<int>(300); // Default to 5 minutes
    public NetworkVariable<int> GameDifficulty = new NetworkVariable<int>(1); // 1 = Easy, 2 = Medium, 3 = Hard

    // Variables affected by difficulty
    public NetworkVariable<int> SpawnedGhostsCount = new NetworkVariable<int>(4);
    public NetworkVariable<float> GhostMinVisibilityDuration = new NetworkVariable<float>(5f);
    public NetworkVariable<float> ParalyzeDuration = new NetworkVariable<float>(8f);
    public NetworkVariable<float> StunDuration = new NetworkVariable<float>(8f);
    public NetworkVariable<float> GhostWalkingSpeed = new NetworkVariable<float>(1f);
    public NetworkVariable<float> LaserRechargeDuration = new NetworkVariable<float>(5f);
    public NetworkVariable<float> FlashlightCooldownDuration = new NetworkVariable<float>(1f);
    public NetworkVariable<float> VacuumDuration = new NetworkVariable<float>(4f);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Set initial difficulty
            UpdateGameDifficulty(GameDifficulty.Value);
        }
    }

    // Updates game difficulty and adjusts related variables
    public void UpdateGameDifficulty(int difficulty)
    {
        if (!IsServer) return; // Only server can change values

        GameDifficulty.Value = difficulty;

        switch (difficulty)
        {
            case 1: // Easy
                SpawnedGhostsCount.Value = 6;
                GhostMinVisibilityDuration.Value = 30f;
                ParalyzeDuration.Value = 10f;
                StunDuration.Value = 10f;
                GhostWalkingSpeed.Value = 0.5f;
                LaserRechargeDuration.Value = 2f;
                FlashlightCooldownDuration.Value = 0.5f;
                VacuumDuration.Value = 2f;
                GameTimeLimit.Value = 500;
                break;

            case 2: // Medium
                SpawnedGhostsCount.Value = 4;
                GhostMinVisibilityDuration.Value = 15f;
                ParalyzeDuration.Value = 8f;
                StunDuration.Value = 8f;
                GhostWalkingSpeed.Value = 1f;
                LaserRechargeDuration.Value = 5f;
                FlashlightCooldownDuration.Value = 1f;
                VacuumDuration.Value = 4f;
                GameTimeLimit.Value = 300;
                break;

            case 3: // Hard
                SpawnedGhostsCount.Value = 1;
                GhostMinVisibilityDuration.Value = 5f;
                ParalyzeDuration.Value = 3f;
                StunDuration.Value = 3f;
                GhostWalkingSpeed.Value = 2f;
                LaserRechargeDuration.Value = 8f;
                FlashlightCooldownDuration.Value = 2f;
                VacuumDuration.Value = 6f;
                GameTimeLimit.Value = 200;
                break;

            default:
                Debug.LogWarning("Invalid difficulty level!");
                break;
        }
    }

    // Sets the game timer (useful for synchronization)
    public void SetGameTime(int time)
    {
        if (IsServer)
        {
            GameTime.Value = time;
        }
    }

    // Gets the game difficulty
    public int GetGameDifficulty()
    {
        return GameDifficulty.Value;
    }

    public int GetCaughtGhostsCount()
    {
        return CaughtGhostsCount.Value;
    }

    // Sets the caught ghosts count (Server Only)
    public void SetCaughtGhostsCount(int count)
    {
        if (IsServer)
        {
            CaughtGhostsCount.Value = count;
        }
    }

    // Increments the caught ghost count (Server Only)
    public void IncrementCaughtGhosts()
    {
        if (IsServer)
        {
            CaughtGhostsCount.Value++;
        }
    }

    // Gets the current game time
    public int GetGameTime()
    {
        return GameTime.Value;
    }

    // Gets the game time limit
    public int GetGameTimeLimit()
    {
        return GameTimeLimit.Value;
    }

    // Sets the game time limit (Server Only)
    public void SetGameTimeLimit(int timeLimit)
    {
        if (IsServer)
        {
            GameTimeLimit.Value = timeLimit;
        }
    }

    // Gets all difficulty properties
    public (int spawnedGhosts, float ghostVisibility, float paralyze, float stun, float ghostSpeed,
            float laserRecharge, float flashlightCooldown, float vacuumDuration) GetDifficultyProperties()
    {
        return (SpawnedGhostsCount.Value, GhostMinVisibilityDuration.Value, ParalyzeDuration.Value,
                StunDuration.Value, GhostWalkingSpeed.Value, LaserRechargeDuration.Value,
                FlashlightCooldownDuration.Value, VacuumDuration.Value);
    }
}
