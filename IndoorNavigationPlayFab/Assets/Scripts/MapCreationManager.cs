using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.XR.ARFoundation;

public class MapCreationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mapCreationUI;
    [SerializeField] private Button placeWaypointButton;
    [SerializeField] private Button endMappingButton;
    [SerializeField] private Button setDestinationButton;
    [SerializeField] private TMP_InputField mapNameInput;
    [SerializeField] private GameObject uploadPanel;
    [SerializeField] private Button uploadButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject waypointPrefab;
    [SerializeField] private GameObject sourceWaypointPrefab;
    [SerializeField] private GameObject destinationWaypointPrefab;
    
    [Header("Settings")]
    [SerializeField] private float minDistanceBetweenWaypoints = 0.5f;
    [SerializeField] private float autoPlacementDistance = 1.0f;
    [SerializeField] private bool useAutoPlacement = true;
    
    private List<GameObject> waypointObjects = new List<GameObject>();
    private MapData currentMap = new MapData();
    private Vector3 lastWaypointPosition;
    private bool isCreatingMap = false;
    private int sourceWaypointIndex = -1;
    private int destinationWaypointIndex = -1;
    private bool isUploading = false;
    private float uploadStartTime = 0;
    private float uploadTimeout = 30f; // 30 seconds timeout
    
    void Start()
    {
        placeWaypointButton.onClick.AddListener(PlaceWaypoint);
        endMappingButton.onClick.AddListener(EndMapping);
        setDestinationButton.onClick.AddListener(SetSelectedWaypointAsDestination);
        uploadButton.onClick.AddListener(UploadMap);
        
        // Initially hide the mapping UI
        mapCreationUI.SetActive(false);
        uploadPanel.SetActive(false);
    }
    
    public void StartMapCreation()
    {
        ARManager.Instance.SetupForMapCreation();
        
        // Reset map data
        currentMap = new MapData();
        waypointObjects.ForEach(wp => Destroy(wp));
        waypointObjects.Clear();
        
        mapCreationUI.SetActive(true);
        uploadPanel.SetActive(false);
        isCreatingMap = true;
        
        // Place the first waypoint as source
        PlaceSourceWaypoint();
    }
    
    private void PlaceSourceWaypoint()
    {
        if (!ARManager.Instance.TryGetPlacementPose(out Pose pose))
            return;
        
        // Create an anchor for stability
        ARAnchor anchor = ARManager.Instance.CreateAnchor(pose);
        
        // Instantiate waypoint parented to the anchor
        GameObject sourceObj;
        if (anchor != null)
        {
            sourceObj = Instantiate(sourceWaypointPrefab, anchor.transform);
            sourceObj.transform.localPosition = Vector3.zero;
            sourceObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            sourceObj = Instantiate(sourceWaypointPrefab, pose.position, pose.rotation);
        }
        
        waypointObjects.Add(sourceObj);
        
        // Add to map data
        WaypointData waypointData = new WaypointData(pose.position, pose.rotation, true, false, "Source");
        currentMap.waypoints.Add(waypointData);
        currentMap.sourceWaypointIndex = 0;
        sourceWaypointIndex = 0;
        
        lastWaypointPosition = pose.position;
    }
    
    private void PlaceWaypoint()
    {
        if (!isCreatingMap)
            return;
            
        if (!ARManager.Instance.TryGetPlacementPose(out Pose pose))
            return;
            
        // Check minimum distance
        if (Vector3.Distance(pose.position, lastWaypointPosition) < minDistanceBetweenWaypoints)
        {
            statusText.text = "Too close to last waypoint";
            return;
        }
        
        // Create an anchor for stability
        ARAnchor anchor = ARManager.Instance.CreateAnchor(pose);
        
        // Instantiate waypoint parented to the anchor
        GameObject waypointObj;
        if (anchor != null)
        {
            waypointObj = Instantiate(waypointPrefab, anchor.transform);
            waypointObj.transform.localPosition = Vector3.zero;
            waypointObj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            waypointObj = Instantiate(waypointPrefab, pose.position, pose.rotation);
        }
        
        waypointObjects.Add(waypointObj);
        
        // Add to map data
        WaypointData waypointData = new WaypointData(pose.position, pose.rotation);
        currentMap.waypoints.Add(waypointData);
        
        lastWaypointPosition = pose.position;
        statusText.text = $"Placed waypoint {waypointObjects.Count}";
    }
    
    // The rest of the code remains largely the same, just make sure to use anchors
    // when creating new waypoints in the SetSelectedWaypointAsDestination method
    
    private void SetSelectedWaypointAsDestination()
    {
        // For simplicity, we'll set the last placed waypoint as destination
        // In a complete app, you'd implement waypoint selection
        
        if (waypointObjects.Count <= 1)
        {
            statusText.text = "Need at least 2 waypoints";
            return;
        }
        
        // If a destination is already set, revert it to normal waypoint
        if (destinationWaypointIndex >= 0 && destinationWaypointIndex < waypointObjects.Count)
        {
            GameObject oldWaypoint = waypointObjects[destinationWaypointIndex];
            Transform parentTransform = oldWaypoint.transform.parent; // This might be an anchor
            
            Destroy(oldWaypoint);
            
            Pose pose = new Pose(
                currentMap.waypoints[destinationWaypointIndex].position,
                currentMap.waypoints[destinationWaypointIndex].rotation
            );
            
            GameObject newWaypoint;
            if (parentTransform != null && parentTransform.GetComponent<ARAnchor>() != null)
            {
                newWaypoint = Instantiate(waypointPrefab, parentTransform);
                newWaypoint.transform.localPosition = Vector3.zero;
                newWaypoint.transform.localRotation = Quaternion.identity;
            }
            else
            {
                newWaypoint = Instantiate(waypointPrefab, pose.position, pose.rotation);
            }
            
            waypointObjects[destinationWaypointIndex] = newWaypoint;
            currentMap.waypoints[destinationWaypointIndex].isDestination = false;
        }
        
        // Set the last waypoint as destination
        int lastIndex = waypointObjects.Count - 1;
        GameObject lastWaypoint = waypointObjects[lastIndex];
        Transform lastParentTransform = lastWaypoint.transform.parent; // This might be an anchor
        
        Destroy(lastWaypoint);
        
        Pose newPose = new Pose(
            currentMap.waypoints[lastIndex].position,
            currentMap.waypoints[lastIndex].rotation
        );
        
        GameObject destWaypoint;
        if (lastParentTransform != null && lastParentTransform.GetComponent<ARAnchor>() != null)
        {
            destWaypoint = Instantiate(destinationWaypointPrefab, lastParentTransform);
            destWaypoint.transform.localPosition = Vector3.zero;
            destWaypoint.transform.localRotation = Quaternion.identity;
        }
        else
        {
            destWaypoint = Instantiate(destinationWaypointPrefab, newPose.position, newPose.rotation);
        }
        
        waypointObjects[lastIndex] = destWaypoint;
        currentMap.waypoints[lastIndex].isDestination = true;
        currentMap.waypoints[lastIndex].label = "Destination";
        
        destinationWaypointIndex = lastIndex;
        currentMap.destinationWaypointIndex = lastIndex;
        
        statusText.text = "Destination set";
    }
    
    private void EndMapping()
    {
        isCreatingMap = false;
        uploadPanel.SetActive(true);
        mapNameInput.text = $"My Map {DateTime.Now:yyyy-MM-dd HH:mm}";
    }
    
    private void UploadMap()
    {
        // In UploadMap method
        if (!NetworkCheck.IsConnectedToInternet())
        {
            statusText.text = "No internet connection. Please check your connectivity.";
            return;
        }

        string mapName = mapNameInput.text;
        if (string.IsNullOrEmpty(mapName))
        {
            statusText.text = "Please enter a map name";
            return;
        }

        if (destinationWaypointIndex < 0)
        {
            statusText.text = "Please set a destination";
            return;
        }

        if (currentMap.waypoints.Count > 100) // If too many waypoints
        {
            statusText.text = "Optimizing map data...";
            OptimizeMapData();
        }

        currentMap.mapName = mapName;

        statusText.text = "Checking PlayFab connection...";

        // First ensure we're authenticated
        if (!PlayFabManager.Instance.IsAuthenticated)
        {
            PlayFabManager.Instance.LoginAnonymously(success => {
                if (success)
                {
                    statusText.text = "Uploading map...";
                    PlayFabManager.Instance.UploadMap(currentMap, OnMapUploaded);
                }
                else
                {
                    statusText.text = "Failed to connect to PlayFab. Check internet connection.";
                }
            });
        }
        else
        {
            statusText.text = "Uploading map...";
            PlayFabManager.Instance.UploadMap(currentMap, OnMapUploaded);
        }

        isUploading = true;
        uploadStartTime = Time.time;
        statusText.text = "Uploading map...";
        PlayFabManager.Instance.UploadMap(currentMap, OnMapUploaded);
    }
    
    private void OptimizeMapData()
    {
        // Reduce precision of coordinates
        for (int i = 0; i < currentMap.waypoints.Count; i++)
        {
            WaypointData wp = currentMap.waypoints[i];
            wp.position = new Vector3(
                Mathf.Round(wp.position.x * 100f) / 100f, // 2 decimal places
                Mathf.Round(wp.position.y * 100f) / 100f,
                Mathf.Round(wp.position.z * 100f) / 100f
            );
            currentMap.waypoints[i] = wp;
        }
    }

    private void OnMapUploaded(bool success, string mapId)
    {
        if (success)
        {
            statusText.text = "Map uploaded successfully!";
            // Return to main menu after a delay
            Invoke(nameof(ReturnToMainMenu), 2f);
        }
        else
        {
            statusText.text = "Failed to upload map: " + mapId;
        }
    }
    
    private void ReturnToMainMenu()
    {
        mapCreationUI.SetActive(false);
        uploadPanel.SetActive(false);
        UIManager.Instance.ShowMainMenu();
    }
    
    void Update()
    {
        if (isCreatingMap && useAutoPlacement)
        {
            // Auto-place waypoints as user moves
            if (!ARManager.Instance.TryGetPlacementPose(out Pose pose))
                return;
                
            if (Vector3.Distance(pose.position, lastWaypointPosition) >= autoPlacementDistance)
            {
                // Create an anchor for stability
                ARAnchor anchor = ARManager.Instance.CreateAnchor(pose);
                
                // Instantiate waypoint parented to the anchor
                GameObject waypointObj;
                if (anchor != null)
                {
                    waypointObj = Instantiate(waypointPrefab, anchor.transform);
                    waypointObj.transform.localPosition = Vector3.zero;
                    waypointObj.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    waypointObj = Instantiate(waypointPrefab, pose.position, pose.rotation);
                }
                
                waypointObjects.Add(waypointObj);
                
                // Add to map data
                WaypointData waypointData = new WaypointData(pose.position, pose.rotation);
                currentMap.waypoints.Add(waypointData);
                
                lastWaypointPosition = pose.position;
                statusText.text = $"Auto-placed waypoint {waypointObjects.Count}";
            }
        }
        if (isUploading && Time.time - uploadStartTime > uploadTimeout)
        {
            statusText.text = "Upload timed out. Please try again.";
            isUploading = false;
        }
    }
}