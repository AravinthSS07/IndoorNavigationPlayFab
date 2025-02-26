using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using System.Collections;

public class NavigationManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject navigationUI;
    [SerializeField] private TMP_Dropdown mapSelectionDropdown;
    [SerializeField] private Button loadMapButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI navigationStatus;
    [SerializeField] private Slider progressBar;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject waypointPrefab;
    [SerializeField] private GameObject sourceWaypointPrefab;
    [SerializeField] private GameObject destinationWaypointPrefab;
    [SerializeField] private GameObject directionIndicatorPrefab;
    
    [Header("Settings")]
    [SerializeField] private float waypointActivationDistance = 2f;
    [SerializeField] private float destinationReachedDistance = 1f;
    [SerializeField] private int maxVisibleWaypoints = 3;
    [SerializeField] private Button createSampleMapButton;
    
    private List<MapCatalogEntry> availableMaps = new List<MapCatalogEntry>();
    private MapData currentMap;
    private List<GameObject> waypointObjects = new List<GameObject>();
    private int currentWaypointIndex = 0;
    private bool isNavigating = false;
    private List<int> visibleWaypointIndices = new List<int>();
    
    void Start()
    {
        mapSelectionDropdown.onValueChanged.AddListener(OnMapSelected);
        loadMapButton.onClick.AddListener(LoadSelectedMap);
        backButton.onClick.AddListener(ReturnToMainMenu);
        
        // Hide navigation UI initially
        navigationUI.SetActive(false);
    }
    
    public void StartNavigationMode()
    {
        navigationUI.SetActive(true);
        LoadAvailableMaps();
    }
    
    public void LoadAvailableMaps()
    {
        navigationStatus.text = "Loading available maps...";
        mapSelectionDropdown.ClearOptions();
        availableMaps.Clear();

        // Disable load button until maps are loaded
        loadMapButton.interactable = false;

        // Check authentication first
        if (!PlayFabManager.Instance.IsAuthenticated)
        {
            navigationStatus.text = "Authenticating with PlayFab...";
            PlayFabManager.Instance.LoginAnonymously(authSuccess => {
                if (authSuccess)
                {
                    navigationStatus.text = "Authentication successful, loading maps...";
                    LoadMapsAfterAuth();
                }
                else
                {
                    navigationStatus.text = "Authentication failed. Please restart the app.";
                }
            });
        }
        else
        {
            LoadMapsAfterAuth();
        }
    }

    private void LoadMapsAfterAuth()
    {
        // Make sure we're on the main thread for UI updates
        navigationStatus.text = "Requesting available maps from PlayFab...";

        // Request maps from PlayFab
        Debug.Log("Requesting available maps from PlayFab...");
        PlayFabManager.Instance.GetMapCatalog(maps => {
            availableMaps = maps;

            Debug.Log($"Received {maps.Count} maps from PlayFab");

            if (maps.Count > 0)
            {
                List<string> options = new List<string>();
                foreach (var map in maps)
                {
                    options.Add($"{map.mapName}");
                    Debug.Log($"Adding map to dropdown: {map.mapName} (ID: {map.mapId})");
                }

                mapSelectionDropdown.ClearOptions();
                mapSelectionDropdown.AddOptions(options);
                navigationStatus.text = $"Found {maps.Count} available maps";
                loadMapButton.interactable = true;
            }
            else
            {
                navigationStatus.text = "No maps available. Create some maps first.";
                mapSelectionDropdown.options.Add(new TMP_Dropdown.OptionData("No maps available"));
                mapSelectionDropdown.RefreshShownValue();
                loadMapButton.interactable = false;

                Debug.LogWarning("No maps were received from PlayFab");

                // Show a button to create a sample map
                if (createSampleMapButton != null)
                    createSampleMapButton.gameObject.SetActive(true);
            }
        });
    }
    
    private void OnMapSelected(int index)
    {
        if (index >= 0 && index < availableMaps.Count)
        {
            navigationStatus.text = $"Selected: {availableMaps[index].mapName}";
        }
    }
    
    private void LoadSelectedMap()
    {
        int selectedIndex = mapSelectionDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < availableMaps.Count)
        {
            var mapEntry = availableMaps[selectedIndex];
            navigationStatus.text = $"Loading map: {mapEntry.mapName}...";
            
            PlayFabManager.Instance.LoadMap(mapEntry.mapId, mapEntry.creatorId, OnMapLoaded);
        }
    }
    
    private void OnMapLoaded(MapData mapData)
    {
        if (mapData == null)
        {
            navigationStatus.text = "Failed to load map";
            return;
        }
        
        currentMap = mapData;
        navigationStatus.text = $"Map '{mapData.mapName}' loaded. Prepare for navigation.";
        
        // Clear any existing waypoints
        ClearWaypoints();
        
        // Set up AR for navigation
        ARManager.Instance.SetupForNavigation();
        
        // Start the navigation process
        StartCoroutine(InitializeNavigation());
    }
    
    private IEnumerator InitializeNavigation()
    {
        // Wait for AR to stabilize
        yield return new WaitForSeconds(2f);
        
        // Place the source waypoint at the user's position
        if (ARManager.Instance.TryGetPlacementPose(out Pose pose))
        {
            // Create waypoint objects but only make the first few visible
            CreateWaypointObjects(pose);
            
            isNavigating = true;
            currentWaypointIndex = 0;
            
            navigationStatus.text = "Follow the waypoints";
            progressBar.maxValue = currentMap.waypoints.Count;
            progressBar.value = 0;
        }
        else
        {
            navigationStatus.text = "Unable to place starting point. Try again.";
        }
    }
    
    private void CreateWaypointObjects(Pose originPose)
    {
        // Create all waypoint objects but initially make them inactive
        for (int i = 0; i < currentMap.waypoints.Count; i++)
        {
            var waypointData = currentMap.waypoints[i];
            Vector3 position = originPose.position;
            Quaternion rotation = originPose.rotation;

            if (i > 0)
            {
                // Calculate position relative to origin for all but first waypoint
                Vector3 relativePos = waypointData.position - currentMap.waypoints[0].position;
                position = originPose.position + relativePos;

                // Maintain relative rotation
                rotation = waypointData.rotation;
            }

            // Create an anchor for stability
            Pose wayPointPose = new Pose(position, rotation);
            ARAnchor anchor = ARManager.Instance.CreateAnchor(wayPointPose);

            GameObject prefab = waypointPrefab;
            if (i == currentMap.sourceWaypointIndex)
                prefab = sourceWaypointPrefab;
            else if (i == currentMap.destinationWaypointIndex)
                prefab = destinationWaypointPrefab;

            GameObject waypointObj;
            if (anchor != null)
            {
                waypointObj = Instantiate(prefab, anchor.transform);
                waypointObj.transform.localPosition = Vector3.zero;
                waypointObj.transform.localRotation = Quaternion.identity;
            }
            else
            {
                waypointObj = Instantiate(prefab, position, rotation);
            }

            waypointObj.SetActive(false);  // Start inactive
            waypointObjects.Add(waypointObj);

            // Place direction indicators between waypoints
            if (i > 0)
            {
                Vector3 previousPos = waypointObjects[i-1].transform.position;
                Vector3 currentPos = waypointObj.transform.position;
                Vector3 direction = currentPos - previousPos;

                GameObject directionIndicator = Instantiate(directionIndicatorPrefab, 
                                                          previousPos + direction * 0.5f, 
                                                          Quaternion.LookRotation(direction));

                directionIndicator.transform.parent = waypointObjects[i-1].transform;
                directionIndicator.SetActive(false);
            }
        }

        // Initially show first few waypoints
        UpdateVisibleWaypoints();
    }
    
    private void UpdateVisibleWaypoints()
    {
        // Hide all waypoints first
        for (int i = 0; i < waypointObjects.Count; i++)
        {
            waypointObjects[i].SetActive(false);
            
            // Also hide direction indicators
            foreach (Transform child in waypointObjects[i].transform)
            {
                if (child.gameObject.CompareTag("DirectionIndicator"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        
        // Show only a few waypoints ahead
        visibleWaypointIndices.Clear();
        for (int i = currentWaypointIndex; i < Mathf.Min(currentWaypointIndex + maxVisibleWaypoints, waypointObjects.Count); i++)
        {
            waypointObjects[i].SetActive(true);
            visibleWaypointIndices.Add(i);
            
            // Show direction indicators for visible waypoints except the last one
            if (i < Mathf.Min(currentWaypointIndex + maxVisibleWaypoints, waypointObjects.Count) - 1)
            {
                foreach (Transform child in waypointObjects[i].transform)
                {
                    if (child.gameObject.CompareTag("DirectionIndicator"))
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
    
    private void CheckWaypointProgress()
    {
        if (!isNavigating || waypointObjects.Count == 0 || currentWaypointIndex >= waypointObjects.Count)
            return;
            
        Vector3 currentPosition = ARManager.Instance.ARCamera.transform.position;
        Vector3 waypointPosition = waypointObjects[currentWaypointIndex].transform.position;
        waypointPosition.y = currentPosition.y; // Ignore height difference for distance calculation
        
        float distance = Vector3.Distance(currentPosition, waypointPosition);
        
        // Check if we've reached the current waypoint
        if (distance <= waypointActivationDistance)
        {
            // Check if we've reached the destination
            if (currentWaypointIndex == currentMap.destinationWaypointIndex)
            {
                if (distance <= destinationReachedDistance)
                {
                    DestinationReached();
                    return;
                }
            }
            
            // Move to next waypoint
            currentWaypointIndex++;
            progressBar.value = currentWaypointIndex;
            
            // Update which waypoints are visible
            if (currentWaypointIndex < waypointObjects.Count)
            {
                navigationStatus.text = $"Waypoint {currentWaypointIndex + 1}/{waypointObjects.Count}";
                UpdateVisibleWaypoints();
            }
        }
    }
    
    private void DestinationReached()
    {
        isNavigating = false;
        navigationStatus.text = "Destination reached!";
        progressBar.value = progressBar.maxValue;
        
        // Show a success message and return to menu after delay
        Invoke(nameof(ReturnToMainMenu), 3f);
    }
    
    private void ReturnToMainMenu()
    {
        isNavigating = false;
        ClearWaypoints();
        navigationUI.SetActive(false);
        UIManager.Instance.ShowMainMenu();
    }
    
    private void ClearWaypoints()
    {
        foreach (var waypoint in waypointObjects)
        {
            Destroy(waypoint);
        }
        waypointObjects.Clear();
        visibleWaypointIndices.Clear();
    }
    
    void Update()
    {
        if (isNavigating)
        {
            CheckWaypointProgress();
        }
    }
}