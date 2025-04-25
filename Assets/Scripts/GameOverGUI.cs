using UnityEngine;
using TMPro;

public class GameOverGUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private bool isMainMenu;
    [SerializeField] private TextMeshProUGUI pointsText;
    
    private void Start()
    {
        // Hide game over panel at start
        if (gameOverPanel != null && !isMainMenu)
        {
            gameOverPanel.SetActive(false);
        }
    }
    
    // Call this method when the game is over
    public static void ShowGameOver(int points)
    {
        
        GameOverGUI instance = FindObjectOfType<GameOverGUI>();
        if (instance == null)
        {
            Debug.LogError("GameOverGUI instance not found!");
            return;
        }

        instance.pointsText.enabled = false;
        
        // Update the score text
        if (instance.scoreText != null)
        {
            instance.scoreText.text = $"You scored {points} Points!";
        }
        
        // Show the game over panel
        if (instance.gameOverPanel != null)
        {
            instance.gameOverPanel.SetActive(true);
        }
    }
    
    // Button functionality for restart
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
    
    // Button functionality for main menu
    public void ReturnToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}