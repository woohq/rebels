using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathGenerator : MonoBehaviour
{
    [Header("Path Generation")]
    [SerializeField] private int numberOfWaypoints = 20;
    [SerializeField] private float waypointSpacing = 10f;
    [SerializeField] private Transform pivotPoint;
    
    [Header("Obstacle Generation")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private float obstacleSpawnChance = 0.7f;
    [SerializeField] private int minObstaclesPerWaypoint = 1;
    [SerializeField] private int maxObstaclesPerWaypoint = 5;
    [SerializeField] private float obstacleChanceDecay = 0.7f; // Each additional obstacle is this much less likely
    
    [Header("Enemy Generation")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float enemySpawnChance = 0.5f;
    [SerializeField] private int maxEnemiesPerWaypoint = 3;
    [SerializeField] private float enemyChanceDecay = 0.7f;
    
    [Header("Decorative Asteroid Generation")]
    [SerializeField] private GameObject altAsteroidPrefab;
    [SerializeField] private float altAsteroidSpawnChance = 0.5f;
    [SerializeField] private int maxAsteroidPairsPerWaypoint = 4;
    [SerializeField] private float asteroidChanceDecay = 0.7f;
    [SerializeField] private float minDistanceFromPath = 100f;
    [SerializeField] private float maxDistanceFromPath = 300f;
    [SerializeField] private float driftSpeedMin = 0.1f;
    [SerializeField] private float driftSpeedMax = 0.5f;
    
    [Header("Placement Settings")]
    [SerializeField] private float rotationRadius = 70f;
    [SerializeField] private int waypointsToSkipAtStart = 10;
    [SerializeField] private int waypointsToSkipAtEnd = 5;
    
    [Header("UI Elements")]
    [SerializeField] private UnityEngine.UI.Image progressBar;
    [SerializeField] private RectTransform shipProgressIndicator;
    
    // Public property to access waypoints
    public List<Transform> Waypoints { get; private set; } = new List<Transform>();
    
    private GameObject waypointsContainer;
    
    void Start()
    {
        CreateWaypointsContainer();
        GeneratePathAndObstacles();
    }
    
    private void CreateWaypointsContainer()
    {
        if (waypointsContainer == null)
        {
            waypointsContainer = new GameObject("WaypointsContainer");
            waypointsContainer.transform.parent = transform;
        }
    }
    
    public void GeneratePathAndObstacles()
    {
        CreateWaypointsContainer();
        CreateWaypoints();
        
        // Use a single coroutine for all generation
        StartCoroutine(GenerateLevel());
    }
    
    private IEnumerator GenerateLevel()
    {
        // Wait for a frame to ensure waypoints are properly set up
        yield return null;
        
        GenerateObstacles();
        GenerateEnemies();
        GenerateAltAsteroids();
        
        InitializePathController();
    }
    
    private void CreateWaypoints()
    {
        ClearWaypoints();
        
        // Start position based on pivot point
        Vector3 currentPosition = pivotPoint != null ? pivotPoint.position : Vector3.zero;
        
        for (int i = 0; i < numberOfWaypoints; i++)
        {
            GameObject waypointObj = new GameObject($"Waypoint_{i}");
            waypointObj.transform.parent = waypointsContainer.transform;
            waypointObj.transform.position = currentPosition;
            
            Waypoints.Add(waypointObj.transform);
            
            // Move to next position along Z axis
            currentPosition += Vector3.forward * waypointSpacing;
        }
    }
    
    private void ClearWaypoints()
    {
        Waypoints.Clear();
        
        if (waypointsContainer != null)
        {
            foreach (Transform child in waypointsContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void InitializePathController()
    {
        if (pivotPoint == null || Waypoints.Count == 0)
        {
            Debug.LogError("PathGenerator: Missing pivotPoint or no waypoints generated!");
            return;
        }
        
        PathController pathController = pivotPoint.GetComponent<PathController>();
        if (pathController == null)
        {
            pathController = pivotPoint.gameObject.AddComponent<PathController>();
        }
        
        pathController.Initialize(Waypoints.ToArray(), progressBar, shipProgressIndicator);
    }
    
    private void GenerateObstacles()
    {
        // Skip waypoints at start and end
        int startIndex = waypointsToSkipAtStart;
        int endIndex = Waypoints.Count - waypointsToSkipAtEnd;
        
        for (int i = startIndex; i < endIndex; i++)
        {
            GenerateEntitiesAtWaypoint(
                i, 
                obstaclePrefab, 
                obstacleSpawnChance, 
                minObstaclesPerWaypoint, 
                maxObstaclesPerWaypoint, 
                obstacleChanceDecay,
                "Obstacle"
            );
        }
    }
    
    private void GenerateEnemies()
    {
        if (enemyPrefab == null) return;
        
        int startIndex = waypointsToSkipAtStart;
        int endIndex = Waypoints.Count - waypointsToSkipAtEnd;
        
        for (int i = startIndex; i < endIndex; i++)
        {
            GenerateEntitiesAtWaypoint(
                i, 
                enemyPrefab, 
                enemySpawnChance, 
                1, 
                maxEnemiesPerWaypoint, 
                enemyChanceDecay,
                "Enemy"
            );
        }
    }
    
    // Generic entity generation method
    private void GenerateEntitiesAtWaypoint(
        int waypointIndex, 
        GameObject prefab, 
        float spawnChance, 
        int minEntities, 
        int maxEntities, 
        float chanceDecay,
        string entityName)
    {
        if (Random.value > spawnChance) return;
        
        List<int> occupiedPositions = GetOccupiedPositionsAtWaypoint(waypointIndex);
        float currentChance = 1f; // First entity has 100% chance if we made it here
        
        // Generate between min and max entities
        for (int i = 0; i < maxEntities; i++)
        {
            // For the first minEntities, always spawn
            bool shouldSpawn = i < minEntities || Random.value <= currentChance;
            
            if (shouldSpawn && occupiedPositions.Count < 6)
            {
                if (TryPlaceEntityAtWaypoint(waypointIndex, prefab, occupiedPositions, entityName))
                {
                    // Reduce chance for next entity
                    currentChance *= chanceDecay;
                }
            }
            else
            {
                break; // Stop spawning if we fail a check or run out of positions
            }
        }
    }
    
    private List<int> GetOccupiedPositionsAtWaypoint(int waypointIndex)
    {
        List<int> occupiedPositions = new List<int>();
        Transform waypointTransform = Waypoints[waypointIndex];
        
        // Check each of the 6 potential positions
        for (int posIndex = 0; posIndex < 6; posIndex++)
        {
            float angle = (posIndex * 60f + 240f) * Mathf.Deg2Rad;
            Vector3 positionToCheck = waypointTransform.position + new Vector3(
                Mathf.Sin(angle) * rotationRadius,
                Mathf.Cos(angle) * rotationRadius,
                0
            );
            
            // Check if any children are close to this position
            foreach (Transform child in waypointTransform)
            {
                if (Vector3.Distance(child.position, positionToCheck) < 5f)
                {
                    occupiedPositions.Add(posIndex);
                    break;
                }
            }
        }
        
        return occupiedPositions;
    }
    
    private bool TryPlaceEntityAtWaypoint(
        int waypointIndex, 
        GameObject prefab, 
        List<int> occupiedPositions, 
        string entityName)
    {
        Transform waypointTransform = Waypoints[waypointIndex];
        
        // Find an unoccupied position
        int maxAttempts = 10;
        int posIndex = -1;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            posIndex = Random.Range(0, 6);
            
            if (!occupiedPositions.Contains(posIndex))
            {
                break; // Found an unoccupied position
            }
            
            if (attempt == maxAttempts - 1)
            {
                return false; // No free positions found
            }
        }
        
        // Mark position as occupied
        occupiedPositions.Add(posIndex);
        
        // Calculate position
        float angle = (posIndex * 60f + 240f) * Mathf.Deg2Rad;
        Vector3 entityPos = waypointTransform.position + new Vector3(
            Mathf.Sin(angle) * rotationRadius,
            Mathf.Cos(angle) * rotationRadius,
            0
        );
        
        // Create entity
        GameObject entity = Instantiate(
            prefab,
            entityPos,
            GetRandomRotation(),
            waypointTransform
        );
        
        // Random scale
        float scaleVariation = Random.Range(0.7f, 1.1f);
        entity.transform.localScale *= scaleVariation;
        
        entity.name = $"{entityName}_Pos{posIndex}";
        
        return true;
    }
    
    private void GenerateAltAsteroids()
    {
        if (altAsteroidPrefab == null) return;
        
        for (int i = 1; i < Waypoints.Count - 1; i++)
        {
            float currentChance = altAsteroidSpawnChance;
            
            for (int pair = 0; pair < maxAsteroidPairsPerWaypoint; pair++)
            {
                if (Random.value <= currentChance)
                {
                    SpawnAltAsteroidPair(i);
                    currentChance *= asteroidChanceDecay;
                }
                else
                {
                    break;
                }
            }
        }
    }
    
    private void SpawnAltAsteroidPair(int waypointIndex)
    {
        if (waypointIndex >= Waypoints.Count) return;
        
        Transform waypointTransform = Waypoints[waypointIndex];
        Vector3 pathPos = waypointTransform.position;
        
        // Generate random angle for first asteroid
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Spawn pair of asteroids on opposite sides
        string id = Random.Range(0, 1000).ToString();
        SpawnSingleAltAsteroid(pathPos, randomAngle, waypointTransform, "A" + id);
        SpawnSingleAltAsteroid(pathPos, randomAngle + Mathf.PI, waypointTransform, "B" + id);
    }
    
    private void SpawnSingleAltAsteroid(Vector3 pathPos, float angleInRadians, Transform parent, string suffix)
    {
        // Direction based on angle
        Vector3 direction = new Vector3(
            Mathf.Cos(angleInRadians),
            Mathf.Sin(angleInRadians),
            0
        ).normalized;
        
        // Random distance
        float distance = Random.Range(minDistanceFromPath, maxDistanceFromPath);
        
        // Calculate position with random variation
        Vector3 asteroidPos = pathPos + (direction * distance) + new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f)
        );
        
        // Create asteroid
        GameObject asteroid = Instantiate(
            altAsteroidPrefab,
            asteroidPos,
            GetRandomRotation(),
            parent
        );
        
        // Random scale with greater variation
        float scaleVariation = Random.Range(0.3f, 3.0f);
        asteroid.transform.localScale *= scaleVariation;
        
        // Add drift component
        AsteroidDrift drift = asteroid.AddComponent<AsteroidDrift>();
        drift?.InitializeDrift(Random.Range(driftSpeedMin, driftSpeedMax));
        
        // Name for organization
        int angleDegrees = Mathf.RoundToInt(angleInRadians * Mathf.Rad2Deg) % 360;
        asteroid.name = $"AltAsteroid_{angleDegrees}deg_{suffix}";
    }
    
    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );
    }
    
    private void OnDrawGizmos()
    {
        if (Waypoints == null || Waypoints.Count == 0) return;
        
        // Draw path
        Gizmos.color = Color.blue;
        for (int i = 0; i < Waypoints.Count - 1; i++)
        {
            if (Waypoints[i] != null && Waypoints[i+1] != null)
            {
                Gizmos.DrawLine(Waypoints[i].position, Waypoints[i+1].position);
            }
        }
        
        // Draw obstacle positions
        Gizmos.color = Color.red;
        foreach (Transform waypoint in Waypoints)
        {
            if (waypoint == null) continue;
            
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + 240f) * Mathf.Deg2Rad;
                
                Vector3 pos = waypoint.position + new Vector3(
                    Mathf.Sin(angle) * rotationRadius,
                    Mathf.Cos(angle) * rotationRadius,
                    0
                );
                
                Gizmos.DrawSphere(pos, 2f);
            }
        }
    }
    
    // Public method to clear the entire level
    public void ClearLevel()
    {
        ClearWaypoints();
    }
    
    // For debugging
    [ContextMenu("Regenerate Level")]
    public void DebugRegenerateLevel()
    {
        ClearLevel();
        GeneratePathAndObstacles();
    }
}