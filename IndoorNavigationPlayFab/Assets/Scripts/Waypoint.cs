using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Waypoint : MonoBehaviour
{
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Material standardMaterial;
    [SerializeField] private Material activeMaterial;
    
    private Renderer renderer;
    private bool isActive = false;
    private ARAnchor arAnchor;
    
    void Start()
    {
        renderer = visualObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = standardMaterial;
        }
        
        // Check if we need to create an AR anchor
        if (gameObject.GetComponent<ARAnchor>() == null)
        {
            // Add anchor component to stabilize this waypoint's position
            arAnchor = gameObject.AddComponent<ARAnchor>();
        }
        else
        {
            arAnchor = gameObject.GetComponent<ARAnchor>();
        }
        
        // Make the waypoint always face the camera
        StartCoroutine(FaceCameraRoutine());
    }
    
    private System.Collections.IEnumerator FaceCameraRoutine()
    {
        while (true)
        {
            if (ARManager.Instance != null && ARManager.Instance.ARCamera != null)
            {
                // Make only the visual part face the camera, not the whole object
                // (which could affect child objects like direction indicators)
                Vector3 dirToCamera = ARManager.Instance.ARCamera.transform.position - visualObject.transform.position;
                dirToCamera.y = 0; // Keep upright, only rotate horizontally
                
                if (dirToCamera != Vector3.zero)
                {
                    visualObject.transform.rotation = Quaternion.LookRotation(dirToCamera);
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Update every 100ms
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        if (renderer != null)
        {
            renderer.material = active ? activeMaterial : standardMaterial;
        }
    }
    
    public bool IsActive()
    {
        return isActive;
    }
}