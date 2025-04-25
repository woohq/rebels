using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour
{
    [SerializeField] private float rotationAngle = 60f; // Rotation angle in degrees
    [SerializeField] private float rotationSpeed = 200f; // Speed of rotation

    [Header("References")]
    [SerializeField] private Transform shipModelTransform; // Reference to the actual ship model

    private Quaternion targetRotation;
    private AudioSource audioSource;
    private bool isRotating = false;

    void Start()
    {
        // Initialize target rotation to current rotation
        targetRotation = transform.rotation;

        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {

        // Check for left button press
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // Calculate the new target rotation
            Quaternion newTarget = transform.rotation * Quaternion.Euler(0, 0, -rotationAngle);

            // Start combined rotation
            StartCoroutine(RotateToPosition(newTarget, 1)); // 1 for clockwise spin
            PlayRotateSound();
        }

        // Check for right button press
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // Calculate the new target rotation
            Quaternion newTarget = transform.rotation * Quaternion.Euler(0, 0, rotationAngle);

            // Start combined rotation
            StartCoroutine(RotateToPosition(newTarget, -1)); // -1 for counterclockwise spin
            PlayRotateSound();
        }

    }

    private IEnumerator RotateToPosition(Quaternion newTarget, int spinDirection)
    {
        // Set rotating flag
        isRotating = true;

        // Store starting values
        Quaternion startPivotRotation = transform.rotation;
        Quaternion startShipRotation = shipModelTransform.localRotation;

        // Calculate exact rotation time based on angle difference and rotation speed
        float angleDifference = Quaternion.Angle(transform.rotation, newTarget);
        float rotationDuration = angleDifference / (rotationSpeed * 60f); // Adjust denominator based on your speed scale

        // Time tracking
        float elapsedTime = 0f;

        while (elapsedTime < rotationDuration)
        {
            // Calculate progress (0 to 1)
            float t = elapsedTime / rotationDuration;

            // Rotate pivot point
            transform.rotation = Quaternion.Slerp(startPivotRotation, newTarget, t);

            // Spin ship model
            if (shipModelTransform != null)
            {
                float spinAngle = t * 360f * spinDirection;
                shipModelTransform.localRotation = startShipRotation * Quaternion.Euler(0, 0, spinAngle);
            }

            // Update time
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final positions are exact
        transform.rotation = newTarget;

        if (shipModelTransform != null)
        {
            shipModelTransform.localRotation = startShipRotation;
        }

        // Update target rotation
        targetRotation = newTarget;

        // Reset rotating flag
        isRotating = false;
    }

    private void PlayRotateSound()
    {
        SoundManager.Instance.PlayPlayerRotate();
    }
}