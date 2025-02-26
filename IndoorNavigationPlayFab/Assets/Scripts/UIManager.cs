using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    
    [Header("Main Menu Buttons")]
    [SerializeField] private Button createMapButton;
    [SerializeField] private Button loadMapButton;
    
    [Header("Managers")]
    [SerializeField] private MapCreationManager mapCreationManager;
    [SerializeField] private NavigationManager navigationManager;

    [SerializeField] private Button testPlayFabButton;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Button testListMapsButton;
    [SerializeField] private Button createSampleMapButton;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Set up button listeners
        createMapButton.onClick.AddListener(OnCreateMapClicked);
        loadMapButton.onClick.AddListener(OnLoadMapClicked);
        
        // Show main menu by default
        ShowMainMenu();

            // Add to existing Start method
        if (testPlayFabButton != null)
            testPlayFabButton.onClick.AddListener(TestPlayFabConnection);

        // Initialize notification text
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);

        if (testListMapsButton != null)
            testListMapsButton.onClick.AddListener(TestListMaps);

        if (createSampleMapButton != null)
            createSampleMapButton.onClick.AddListener(CreateSampleMap);
    }
    
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        // Make sure AR session is stopped when in menu
        if (ARManager.Instance != null)
        {
            ARManager.Instance.StopARSession();
        }
    }
    
    private void OnCreateMapClicked()
    {
        mainMenuPanel.SetActive(false);
        mapCreationManager.StartMapCreation();
    }
    
    private void OnLoadMapClicked()
    {
        mainMenuPanel.SetActive(false);
        navigationManager.StartNavigationMode();
    }

    public void TestPlayFabConnection()
    {
        PlayFabManager.Instance.TestPlayFabUpload();
    }

    public void ShowNotification(string message, float duration = 5.0f)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);

            // Hide notification after duration
            StartCoroutine(HideNotificationAfterDelay(duration));
        }
        else
        {
            Debug.Log($"UI Notification: {message}");
        }
    }

    private System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    public void TestListMaps()
    {
        PlayFabManager.Instance.TestMapRetrieval();
    }

    public void CreateSampleMap()
    {
        // Create a simple map with one source and one destination
        MapData sampleMap = new MapData();
        sampleMap.mapName = "Sample Map " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        
        // Create source waypoint
        WaypointData sourceWaypoint = new WaypointData(
            new Vector3(0, 0, 0),
            Quaternion.identity,
            true, false, "Source"
        );
        sourceWaypoint.PrepareForSerialization();
        sampleMap.waypoints.Add(sourceWaypoint);
        sampleMap.sourceWaypointIndex = 0;
        
        // Create destination waypoint
        WaypointData destWaypoint = new WaypointData(
            new Vector3(0, 0, 2),
            Quaternion.identity,
            false, true, "Destination"
        );
        destWaypoint.PrepareForSerialization();
        sampleMap.waypoints.Add(destWaypoint);
        sampleMap.destinationWaypointIndex = 1;
        
        // Upload the sample map
        ShowNotification("Creating sample map...");
        PlayFabManager.Instance.UploadMap(sampleMap, (success, mapId) => {
            if (success)
            {
                ShowNotification($"Sample map created successfully! ID: {mapId}");
            }
            else
            {
                ShowNotification($"Failed to create sample map: {mapId}");
            }
        });
    }
}