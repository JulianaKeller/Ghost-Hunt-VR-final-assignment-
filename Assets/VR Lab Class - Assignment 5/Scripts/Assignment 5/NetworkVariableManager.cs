using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkVariableManager : NetworkBehaviour
{
    //Follows the singleton pattern, so other scripts can get the NetworkVariableManager with NetworkVariableManager.Instance
    public static NetworkVariableManager Instance;

    // Game-wide variables
    public NetworkVariable<int> CaughtGhostsCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> GameTime = new NetworkVariable<int>(0);
    public NetworkVariable<int> GameTimeLimit = new NetworkVariable<int>(300); // Default to 5 minutes
    public NetworkVariable<int> GameDifficulty = new NetworkVariable<int>(2); // 1 = Easy, 2 = Medium, 3 = Hard

    // Variables affected by difficulty
    public NetworkVariable<int> SpawnGhostsCount = new NetworkVariable<int>(4);
    public NetworkVariable<float> GhostMinVisibilityDuration = new NetworkVariable<float>(5f);
    public NetworkVariable<float> ParalyzeDuration = new NetworkVariable<float>(8f);
    public NetworkVariable<float> StunDuration = new NetworkVariable<float>(8f);
    public NetworkVariable<float> GhostWalkingSpeed = new NetworkVariable<float>(1f);
    public NetworkVariable<float> LaserGunMaxCharge = new NetworkVariable<float>(4f);
    public NetworkVariable<float> LaserGunDechargeRate = new NetworkVariable<float>(1f);
    public NetworkVariable<float> LaserGunRechargeRate = new NetworkVariable<float>(1f);
    public NetworkVariable<float> FlashlightCooldownDuration = new NetworkVariable<float>(1f);
    public NetworkVariable<float> VacuumMaxCharge = new NetworkVariable<float>(4f);
    public NetworkVariable<float> VacuumDechargeRate = new NetworkVariable<float>(1f);
    public NetworkVariable<float> VacuumRechargeRate = new NetworkVariable<float>(1f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); //Prevents multiple instances
        }
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
                SpawnGhostsCount.Value = 6;
                GhostMinVisibilityDuration.Value = 30f;
                ParalyzeDuration.Value = 10f;
                StunDuration.Value = 10f;
                GhostWalkingSpeed.Value = 0.5f;
                FlashlightCooldownDuration.Value = 0.5f;
                VacuumMaxCharge.Value = 10f;
                VacuumDechargeRate.Value = 1f;
                GameTimeLimit.Value = 500;
                LaserGunMaxCharge.Value = 10f;
                LaserGunDechargeRate.Value = 1f;
                LaserGunRechargeRate.Value = 1f;
                VacuumRechargeRate.Value = 1f;
                break;

            case 2: // Medium
                SpawnGhostsCount.Value = 4;
                GhostMinVisibilityDuration.Value = 15f;
                ParalyzeDuration.Value = 8f;
                StunDuration.Value = 8f;
                GhostWalkingSpeed.Value = 1f;
                FlashlightCooldownDuration.Value = 1f;
                VacuumMaxCharge.Value = 8f;
                VacuumDechargeRate.Value = 1f;
                GameTimeLimit.Value = 300;
                VacuumRechargeRate.Value = 1f;
                LaserGunMaxCharge.Value = 8f;
                LaserGunDechargeRate.Value = 1f;
                LaserGunRechargeRate.Value = 1f;
                VacuumRechargeRate.Value = 1f;
                break;

            case 3: // Hard
                SpawnGhostsCount.Value = 1;
                GhostMinVisibilityDuration.Value = 5f;
                ParalyzeDuration.Value = 3f;
                StunDuration.Value = 3f;
                GhostWalkingSpeed.Value = 2f;
                FlashlightCooldownDuration.Value = 2f;
                VacuumMaxCharge.Value = 6f;
                GameTimeLimit.Value = 200;
                VacuumDechargeRate.Value = 1f;
                VacuumRechargeRate.Value = 1f;
                LaserGunMaxCharge.Value = 6f;
                LaserGunDechargeRate.Value = 1f;
                LaserGunRechargeRate.Value = 1f;
                VacuumRechargeRate.Value = 1f;
                break;

            default:
                Debug.LogWarning("Invalid difficulty level!");
                break;
        }
    }

    public void SetGameTime(int time)
    {
        if (IsServer)
        {
            GameTime.Value = time;
        }
    }

    public int GetGameDifficulty()
    {
        return GameDifficulty.Value;
    }

    public void SetGameDifficulty(int newDifficulty)
    {
        if (IsServer)
        {
            GameDifficulty.Value = newDifficulty;
            UpdateGameDifficulty(newDifficulty);
            Debug.Log("Game difficulty set to " + newDifficulty);
        }
        else
        {
            SetGameDifficultyServerRpc(newDifficulty);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGameDifficultyServerRpc(int newDifficulty)
    {
        GameDifficulty.Value = newDifficulty;
        UpdateGameDifficulty(newDifficulty);
        Debug.Log("Game difficulty updated by server to " + newDifficulty);
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

    public void GhostCaptured()
    {
        if (IsServer)
        {
            CaughtGhostsCount.Value += 1;
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
    public (int SpawnGhostsCount, 
        float GhostMinVisibilityDuration, 
        float ParalyzeDuration, 
        float StunDuration, 
        float GhostWalkingSpeed,
        float FlashlightCooldownDuration, 
        float VacuumMaxCharge, 
        float VacuumDechargeRate, 
        float VacuumRechargeRate,
        float LaserGunMaxCharge,
        float LaserGunDechargeRate,
        float LaserGunRechargeRate) GetDifficultyProperties()
    {
        return (SpawnGhostsCount.Value, 
                GhostMinVisibilityDuration.Value, 
                ParalyzeDuration.Value,
                StunDuration.Value, 
                GhostWalkingSpeed.Value,
                FlashlightCooldownDuration.Value, 
                VacuumMaxCharge.Value, 
                VacuumDechargeRate.Value, 
                VacuumRechargeRate.Value,
                LaserGunMaxCharge.Value,
                LaserGunDechargeRate.Value,
                LaserGunRechargeRate.Value);
    }
}