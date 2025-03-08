using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameTimeManager : NetworkBehaviour
{
    private void Start()
    {
        // Only the server will handle the game time updates
        if (IsServer)
        {
            StartCoroutine(UpdateGameTime());
        }
        else
        {
            Debug.Log("Not server");
        }
    }

    // Coroutine to update the game time every second
    private IEnumerator UpdateGameTime()
    {
        while (true)
        {
            // Wait for 1 second
            yield return new WaitForSeconds(1f);

            // Update game time on the server
            if (IsServer)
            {
                Debug.Log("Updating Game Time...");
                Debug.Log(NetworkVariableManager.Instance == null);
                // Get the current game time and game time limit from the NetworkVariableManager
                int currentTime = NetworkVariableManager.Instance.GetGameTime();
                int gameTimeLimit = NetworkVariableManager.Instance.GetGameTimeLimit();

                // Increase game time by 1 second
                currentTime += 1;

                // Set the new game time
                NetworkVariableManager.Instance.SetGameTime(currentTime);

                // If game time exceeds the game time limit, end the game
                if (currentTime >= gameTimeLimit)
                {
                    EndGame();
                    break; // Stop the coroutine after the game ends
                }
            }
            else
            {
                Debug.Log("Not server");
            }
        }
    }

    // Ends the game (this could trigger various game-ending logic)
    private void EndGame()
    {
        // For now, we'll just log the game over message
        Debug.Log("Game Over! Time has run out.");

        // You can call methods here to end the game, such as:
        // - Show game over UI
        // - Notify all players
        // - Stop game logic (e.g., disable player movement)

        // Example of notifying all clients (could be expanded with events or game state changes)
        NetworkVariableManager.Instance.SetGameTimeLimit(0); // Prevent further updates, for example.
    }
}
