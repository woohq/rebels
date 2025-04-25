using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Player : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    [Header("Points Settings")]
    [SerializeField] private int pointsPerHit = 100; 
    [SerializeField] private TextMeshProUGUI pointsText; 
    public int points; // Keep public for compatibility
    
    [Header("UI Elements")]
    [SerializeField] private RawImage[] healthIcons;
    
    [Header("Effects")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject shipExplosionPrefab;
    [SerializeField] private float invincibilityDuration = 1.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip hurtSound; 
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip pointsSound; 
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask shipLayer;
    
    [Header("References")]
    [SerializeField] private Transform mainCam;
    
    [SerializeField] private PathController pathController;
    [SerializeField] private MeshRenderer shipMesh;

    private AudioSource audioSource;
    private AudioSource explosionAudioSource;
    private bool isInvincible = false;
    

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeComponents();
        
        // Initialize health and points
        currentHealth = maxHealth;
        points = 0;
        
        UpdateHealthUI();
        UpdatePointsUI();
    }
    
    private void InitializeComponents()
    {
        // Get audio sources
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 1.5f;
        }
        
        explosionAudioSource = gameObject.AddComponent<AudioSource>();
        explosionAudioSource.volume = 0.3f;
        
        // Ensure this object has a collider
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check for obstacle collision
        if (obstacleLayer != 0 && ((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            TakeDamage();
        }
        
        // Check for ship collision
        if (shipLayer != 0 && ((1 << other.gameObject.layer) & shipLayer) != 0)
        {
            DestroyShip(other.gameObject);
        }
    }
    
    public void TakeDamage()
    {
        if (isInvincible) return;
        
        currentHealth--;
        UpdateHealthUI();
        
        // Create explosion with slight offset from transform position
        CreateExplosion(transform.position + (transform.forward * 0.5f), explosionPrefab);
        PlaySounds(explosionSound, hurtSound);
        
        if (currentHealth <= 0)
        {
            GameOver();
        }
        else
        {
            // Start invincibility
            StartCoroutine(ApplyInvincibility());
        }
    }
    
    public void DestroyShip(GameObject ship)
    {
        if (ship == null) return;
        
        // Add points
        points += pointsPerHit;
        UpdatePointsUI();
        
        // Create explosion at the ship's position
        CreateExplosion(ship.transform.position, shipExplosionPrefab);
        PlaySounds(explosionSound, pointsSound);
        
        // Destroy the ship
        Destroy(ship);
    }
    
    private void CreateExplosion(Vector3 position, GameObject prefab)
    {
        if (prefab == null) return;
        
        GameObject explosion = Instantiate(prefab, position, Quaternion.identity);
        Destroy(explosion, 2f); // Auto-destroy after 2 seconds
    }
    
    private void PlaySounds(AudioClip effect, AudioClip notification)
    {
        if (explosionAudioSource != null && effect != null)
        {
            explosionAudioSource.PlayOneShot(effect);
        }
        
        if (audioSource != null && notification != null)
        {
            audioSource.PlayOneShot(notification);
        }
    }
    
    private IEnumerator ApplyInvincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    
    void UpdateHealthUI()
    {
        if (healthIcons == null) return;
        
        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] != null)
            {
                healthIcons[i].enabled = i < currentHealth;
            }
        }
    }

    void UpdatePointsUI()
    {
        if (pointsText != null)
        {
            pointsText.text = points.ToString();
        }
    }
    
    void GameOver()
    {
        // Play game over sound
        if (gameOverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gameOverSound);
        }
        
        // Stop path movement
        if (pathController != null)
        {
            CreateExplosion(shipMesh.transform.position, shipExplosionPrefab);
            pathController.setDead();
            shipMesh.enabled = false;
        }
        
        // Show game over GUI
        GameOverGUI.ShowGameOver(points);
        
        Debug.Log("Game Over!");
    }
    
    public void AddPoints(int amount)
    {
        points += amount;
        UpdatePointsUI();
    }

    public void ResetPosition()
    {
        PathGenerator pathGen = FindObjectOfType<PathGenerator>();
        if (pathGen != null && pathGen.Waypoints.Count > 0)
        {
            // Get the first waypoint position
            Vector3 waypointPosition = pathGen.Waypoints[0].position;
            
            // Set player position at the bottom rotation point (Y = -70)
            Vector3 spawnPosition = waypointPosition + new Vector3(0, -70, 0);
            
            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;
            
            if (mainCam != null)
            {
                mainCam.rotation = Quaternion.identity;
            }
        }
        else
        {
            Debug.LogError("Cannot reset player position - no waypoints found!");
        }
    }
    
    // Keep the static class for compatibility
    public static class PlayerData
    {
        public static int Points { get; set; }
        public static int CurrentHealth { get; set; }
        public static int MaxHealth { get; set; }
        public static bool HasInitialized { get; set; } = false;
    }

    // Save player data before switching scenes
    public void SavePlayerData()
    {
        PlayerData.Points = points;
        PlayerData.CurrentHealth = currentHealth;
        PlayerData.MaxHealth = maxHealth;
        PlayerData.HasInitialized = true;
    }

    // Load player data when entering a new scene
    public void LoadPlayerData()
    {
        if (PlayerData.HasInitialized)
        {
            points = PlayerData.Points;
            currentHealth = PlayerData.CurrentHealth;
            maxHealth = PlayerData.MaxHealth;
            
            UpdateHealthUI();
            UpdatePointsUI();
        }
    }
}