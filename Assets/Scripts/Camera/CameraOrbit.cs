using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField]
    private Transform target; // The target object to orbit around        
    [SerializeField] 
    private float distance = 10.0f; // Distance from the target object
    [SerializeField]
    private float orbitSpeed = 10.0f; // Speed of the orbit
    [SerializeField]
    private float polarAngle = 45f;
    [SerializeField]
    private float currentAzimuth = 0.0f; // Current angle of rotation around the target

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not set for CameraOrbit script.");
            enabled = false;
            return;
        }

        // Position the camera at the starting position
        UpdateCameraPosition();
    }

    void Update()
    {
        // Calculate the new angle based on the orbit speed and time
        currentAzimuth += orbitSpeed * Time.deltaTime;
        polarAngle = Mathf.Clamp(polarAngle, 0.0f, 90.0f);

        // Update the camera position and rotation
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        // Convert the current angle to radians
        float azimuthInRadians = currentAzimuth * Mathf.Deg2Rad;
        float polarInRadians = polarAngle * Mathf.Deg2Rad;
        //float height = 

        // Calculate the new position of the camera
        float x = target.position.x + distance * Mathf.Cos(azimuthInRadians) * Mathf.Cos(polarInRadians);
        float z = target.position.z + distance * Mathf.Sin(azimuthInRadians) * Mathf.Cos(polarInRadians);
        float y = target.position.y + distance * Mathf.Sin(polarInRadians); // Maintain the same height as the target

        // Set the new position
        transform.position = new Vector3(x, y, z);

        // Make the camera look at the target
        transform.LookAt(target);
    }
}