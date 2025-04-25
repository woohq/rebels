using UnityEngine;
using TMPro;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathGenerator pathGenerator;
    [SerializeField] private Player player;
    [SerializeField] private TextMeshProUGUI levelCounterText;
    
    [Header("Level Settings")]
    [SerializeField] private int levelCount = 0;
    [SerializeField] private int difficulty = 1;
    [SerializeField] private float playerMoveSpeed = 900f; // Default speed that should be restored
    
    // This will be directly manipulated
    public bool isTransitioning = false;
    
    private void Start()
    {
        Debug.Log("LevelManager: Start called");
        
        // Find references if not assigned
        if (pathGenerator == null)
            pathGenerator = FindObjectOfType<PathGenerator>();
            
        if (player != null)
            player = FindObjectOfType<Player>();
        
        // Force reset transition state on start
        isTransitioning = false;
        
        // Update level counter if available
        UpdateLevelCounter();
    }
    
    public void CompletePath()
{
    // Check if already transitioning to prevent multiple calls
    if (isTransitioning) 
    {
        return;
    }
    
    // Set the flag first thing to prevent multiple calls
    isTransitioning = true;
    
    // Increase level count and difficulty
    levelCount++;
    difficulty++;
    
    // Save player data
    if (player != null)
    {
        player.SavePlayerData();
    }
    
    // Start the transition
    StartCoroutine(TransitionToNextLevel());
}

private IEnumerator TransitionToNextLevel()
{
    // Get warp transition from player if available
    WarpSpeedTransition warpTransition = player?.GetComponent<WarpSpeedTransition>();
    
    if (warpTransition != null)
    {
        // Subscribe to warp events
        warpTransition.OnWarpMidpoint += OnWarpMidpoint;
        warpTransition.OnWarpComplete += OnWarpComplete;
        
        // Start warp effect
        warpTransition.StartWarpEffect();
        
        // Wait for the entire warp duration - we'll handle the midpoint and completion via events
        float totalWarpDuration = warpTransition.GetWarpDuration();
        yield return new WaitForSeconds(totalWarpDuration);
        
        // Unsubscribe from events (clean up)
        warpTransition.OnWarpMidpoint -= OnWarpMidpoint;
        warpTransition.OnWarpComplete -= OnWarpComplete;
    }
    else
    {
        // Fallback if no warp transition is available
        RegenerateLevel();
        ResetPlayerPosition();
        
        // Wait a moment before completing the transition
        yield return new WaitForSeconds(1.0f);
    }
    
    // CRITICAL: Reset the state flag at the very end
    isTransitioning = false;
    }

    // Event handlers
    private void OnWarpMidpoint()
    {
        // Generate the new level during the midpoint of the warp
        RegenerateLevel();
    }

    private void OnWarpComplete()
    {
        // Reset player position after the warp is complete
        ResetPlayerPosition();
    }

    private void ResetPlayerPosition()
    {
        if (player != null)
        {
            player.ResetPosition();
            player.LoadPlayerData();
            
            // Reset the path controller on the player
            PathController pathController = player.GetComponent<PathController>();
            if (pathController != null)
            {
                pathController.Reset();
                
                // IMPORTANT: Make sure to restore the player's speed
                pathController.moveSpeed = playerMoveSpeed;
            }
        }
    }
    
    private void RegenerateLevel()
    {
        if (pathGenerator == null)
        {
            Debug.LogError("PathGenerator reference is missing!");
            return;
        }
        
        Debug.Log("Regenerating level...");
        
        // Clear existing level objects
        pathGenerator.ClearLevel();
        
        // Generate new level with increased difficulty
        pathGenerator.GeneratePathAndObstacles();
        
        // Update level counter
        UpdateLevelCounter();
        
        Debug.Log("New level generated!");
    }
    
    private void UpdateLevelCounter()
    {
        if (levelCounterText != null)
        {
            levelCounterText.text = "Level: " + (levelCount + 1).ToString();
        }
    }
    
    // For emergency use - can be called from the inspector if needed
    [ContextMenu("Force Reset Transition State")]
    private void ForceResetTransitionState()
    {
        isTransitioning = false;
        Debug.Log("Transition state forcefully reset to FALSE");
    }
}