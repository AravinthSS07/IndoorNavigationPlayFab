using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MapCreator : MonoBehaviour
{
    public ARRaycastManager RaycastManager;
    public GameObject BreadcrumbPrefab;
    public List<Vector3> BreadcrumbPositions = new List<Vector3>(); // Store breadcrumb positions
    private GameObject sourceBreadcrumb; // To store the initial breadcrumb (source)

    private bool isMapping = false;

    void Update()
    {
        if (!isMapping) return; // Only proceed if mapping is active

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            RaycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.Planes);

            if (hits.Count > 0)
            {
                Pose hitPose = hits[0].pose;
                // Offset the breadcrumb position slightly backward from the hit position
                Vector3 breadcrumbPosition = hitPose.position - transform.forward * 0.5f; // Adjust offset as needed

                GameObject breadcrumb = Instantiate(BreadcrumbPrefab, breadcrumbPosition, Quaternion.identity);
                BreadcrumbPositions.Add(breadcrumbPosition);

                if (BreadcrumbPositions.Count == 1) // First breadcrumb is the source
                {
                    sourceBreadcrumb = breadcrumb;
                    // Optionally, visually distinguish the source breadcrumb here
                }
            }
        }
    }

    public void StartMapping()
    {
        isMapping = true;
        BreadcrumbPositions.Clear(); // Clear previous breadcrumbs when starting a new map
        if (sourceBreadcrumb != null)
        {
            Destroy(sourceBreadcrumb); // Destroy previous source breadcrumb
            sourceBreadcrumb = null;
        }
        // Optionally, clear any existing breadcrumbs from the scene visually if needed
        GameObject[] existingBreadcrumbs = GameObject.FindGameObjectsWithTag("Breadcrumb"); // Tag breadcrumbs later
        foreach(GameObject bc in existingBreadcrumbs) {
            Destroy(bc);
        }
    }

    public void StopMapping()
    {
        isMapping = false;
        Debug.Log("Mapping stopped. Breadcrumb positions count: " + BreadcrumbPositions.Count);
        // Here you would typically process the BreadcrumbPositions list, e.g., for saving or further actions
    }
}