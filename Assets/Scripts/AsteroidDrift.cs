using UnityEngine;

public class AsteroidDrift : MonoBehaviour
{
    private float driftSpeed;
    private Vector3 driftDirection;
    private float rotationSpeed;
    
    public void InitializeDrift(float speed)
    {
        driftSpeed = speed;
        
        // Random drift direction
        driftDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-0.3f, 0.3f)  // Smaller Z movement
        ).normalized;
        
        // Random rotation speed for tumbling effect
        rotationSpeed = Random.Range(2f, 10f);
    }
    
    void Update()
    {
        // Apply drift movement
        transform.position += driftDirection * driftSpeed * Time.deltaTime;
        
        // Apply tumbling rotation
        transform.Rotate(
            Random.Range(-1f, 1f) * rotationSpeed * Time.deltaTime,
            Random.Range(-1f, 1f) * rotationSpeed * Time.deltaTime,
            Random.Range(-1f, 1f) * rotationSpeed * Time.deltaTime
        );
    }
}