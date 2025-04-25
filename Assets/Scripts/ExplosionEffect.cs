using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private AudioClip explosionSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        // Play explosion sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (explosionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(explosionSound);
        }
        
        // Set a timer to destroy this gameobject
        Destroy(gameObject, lifetime);
    }
}