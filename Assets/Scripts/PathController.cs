using UnityEngine;
using UnityEngine.UI;

public class PathController : MonoBehaviour
{
    public float moveSpeed = 1200f; 
    private float defaultMoveSpeed = 900f;
    
    [Header("Level Manager Reference")]
    [SerializeField] private LevelManager levelManager;
    
    private Transform[] waypoints;
    private Image progressBar;
    private RectTransform shipProgressIndicator;
    
    private int currentWaypointIndex = 0;
    private float totalPathLength;
    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    private bool isInitialized = false;
    private bool isPathComplete = false;
    private bool isDead = false;
    
    void Start()
    {
        defaultMoveSpeed = moveSpeed;
        
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }
        
        isPathComplete = false;
        isDead = false;
    }
    
    public void Initialize(Transform[] waypointArray, Image progressBar, RectTransform shipProgressIndicator)
    {
        if (waypointArray == null || waypointArray.Length == 0)
        {
            Debug.LogError("PathController: No waypoints provided!");
            return;
        }
        
        this.waypoints = waypointArray;
        this.progressBar = progressBar;
        this.shipProgressIndicator = shipProgressIndicator;
        
        // Reset state flags directly, don't call ResetState()
        isPathComplete = false;
        isDead = false;
        currentWaypointIndex = 0;
        distanceTraveled = 0f;
        
        // Make sure speed is properly set
        if (moveSpeed <= 0)
        {
            moveSpeed = defaultMoveSpeed;
        }
        
        // Calculate path length
        CalculateTotalPathLength();
        
        // Start at first waypoint
        transform.position = waypoints[0].position;
        lastPosition = transform.position;
        
        // Set isInitialized to true AFTER all setup is done
        isInitialized = true;
    }
    
    void FixedUpdate()
    {
        UpdateProgressIndicator();
        MoveAlongPath(Time.fixedDeltaTime);
    }
    
    void MoveAlongPath(float deltaTime)
    {
        // Don't move if not initialized or no waypoints or reached end
        if (!isInitialized || waypoints == null || waypoints.Length == 0 || 
            isPathComplete || moveSpeed <= 0 || isDead)
        {
            return;
        }
        
        // Calculate distance to move this frame
        float moveDistance = moveSpeed * deltaTime;
        
        // Store current position
        lastPosition = transform.position;
        
        // Calculate direction to current waypoint
        Vector3 movementDirection = (waypoints[currentWaypointIndex].position - transform.position).normalized;
        
        // Calculate new position
        Vector3 newPosition = transform.position + (movementDirection * moveDistance);
        
        // Check if we would overshoot the waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        
        if (moveDistance >= distanceToWaypoint)
        {
            // Move to the waypoint exactly
            transform.position = waypoints[currentWaypointIndex].position;
            
            // Calculate remaining distance
            float remainingDistance = moveDistance - distanceToWaypoint;
            
            // Move to next waypoint
            currentWaypointIndex++;
            
            // Check if we've reached the end of path
            if (currentWaypointIndex >= waypoints.Length)
            {
                // Path completed
                isPathComplete = true;
                
                // Stop movement
                moveSpeed = 0;
                
                // Notify level manager
                if (levelManager != null)
                {
                    levelManager.CompletePath();
                }
            }
            else
            {
                // Continue movement with remaining distance
                movementDirection = (waypoints[currentWaypointIndex].position - transform.position).normalized;
                transform.position += movementDirection * remainingDistance;
            }
        }
        else
        {
            // Normal movement within segment
            transform.position = newPosition;
        }
        
        // Update distance for progress bar
        distanceTraveled += Vector3.Distance(lastPosition, transform.position);
    }
    
    void CalculateTotalPathLength()
    {
        totalPathLength = 0f;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            totalPathLength += Vector3.Distance(waypoints[i].position, waypoints[i + 1].position);
        }
    }
    
    void UpdateProgressIndicator()
    {
        if (progressBar == null || shipProgressIndicator == null || totalPathLength <= 0f) 
            return;
        
        float progress = Mathf.Clamp01(distanceTraveled / totalPathLength);
        progressBar.fillAmount = progress;
        
        float width = progressBar.rectTransform.rect.width;
        shipProgressIndicator.anchoredPosition = new Vector2(width * progress, 0);
    }
    
    // Reset the controller (used when generating a new level)
    public void Reset()
    {
        isInitialized = false;
        isPathComplete = false;
        isDead = false;
        currentWaypointIndex = 0;
        distanceTraveled = 0f;
        moveSpeed = defaultMoveSpeed;
        waypoints = null;
    }
    
    public void setDead() 
    {
        isDead = true;
    }
}