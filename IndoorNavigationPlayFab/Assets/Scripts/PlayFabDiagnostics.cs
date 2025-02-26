using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

public class PlayFabDiagnostics : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button runDiagnosticsButton;
    [SerializeField] private Button clearLogButton;
    
    private void Start()
    {
        if (runDiagnosticsButton != null)
            runDiagnosticsButton.onClick.AddListener(RunDiagnostics);
            
        if (clearLogButton != null)
            clearLogButton.onClick.AddListener(() => outputText.text = "");
    }
    
    public void RunDiagnostics()
    {
        outputText.text = "Starting PlayFab diagnostics...\n";
        
        // Check authentication
        CheckAuthentication();
    }
    
    private void CheckAuthentication()
    {
        bool isAuthenticated = PlayFabManager.Instance != null && PlayFabManager.Instance.IsAuthenticated;
        
        Log($"Authentication status: {(isAuthenticated ? "Authenticated" : "Not Authenticated")}");
        Log($"PlayFab ID: {(isAuthenticated ? PlayFabManager.Instance.PlayFabId : "None")}");
        
        if (!isAuthenticated)
        {
            Log("Attempting to authenticate...");
            PlayFabManager.Instance.LoginAnonymously(success => {
                Log($"Authentication attempt result: {(success ? "Success" : "Failed")}");
                if (success)
                {
                    // Continue diagnostics
                    CheckUserData();
                }
            });
        }
        else
        {
            // Continue diagnostics
            CheckUserData();
        }
    }
    
    private void CheckUserData()
    {
        Log("Checking user data...");
        
        var request = new GetUserDataRequest();
        
        PlayFabClientAPI.GetUserData(request,
            result => {
                Log($"Retrieved {result.Data.Count} user data items");
                
                // Check for map data
                int mapCount = 0;
                foreach (var item in result.Data)
                {
                    if (item.Key.StartsWith("Map_"))
                    {
                        mapCount++;
                        Log($"Found map data: {item.Key}");
                        
                        // Output first 100 chars of data to see format
                        string preview = item.Value.Value;
                        if (preview.Length > 100)
                            preview = preview.Substring(0, 100) + "...";
                        Log($"Data preview: {preview}");
                    }
                }
                
                if (mapCount == 0)
                    Log("No map data found in user data!");
                    
                // Check for map catalog
                if (result.Data.ContainsKey("MapCatalog"))
                {
                    Log("Found MapCatalog entry");
                    string catalogPreview = result.Data["MapCatalog"].Value;
                    if (catalogPreview.Length > 100)
                        catalogPreview = catalogPreview.Substring(0, 100) + "...";
                    Log($"Catalog preview: {catalogPreview}");
                }
                else
                {
                    Log("No MapCatalog entry found!");
                }
                
                // Check CloudScript
                CheckCloudScript();
            },
            error => {
                Log($"Error retrieving user data: {error.ErrorMessage}");
            }
        );
    }
    
    private void CheckCloudScript()
    {
        Log("Checking CloudScript functionality...");
        
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "getMapCatalog",
            GeneratePlayStreamEvent = true
        };
        
        PlayFabClientAPI.ExecuteCloudScript(request,
            result => {
                if (result.Error != null)
                {
                    Log($"CloudScript error: {result.Error.Message}");
                }
                else
                {
                    Log("CloudScript executed successfully");
                    Log($"Result: {result.FunctionResult?.ToString() ?? "null"}");
                }
                
                // Create a test map entry
                CreateTestMapEntry();
            },
            error => {
                Log($"Error executing CloudScript: {error.ErrorMessage}");
                // Create a test map entry anyway
                CreateTestMapEntry();
            }
        );
    }
    
    private void CreateTestMapEntry()
    {
        Log("Creating a test map entry...");
        
        // Create a simple test map
        string testMapId = "test_" + System.DateTime.UtcNow.Ticks;
        
        // Create a very minimal map data
        var testMapData = new Dictionary<string, string>
        {
            { "Map_" + testMapId, "{\"mapId\":\"" + testMapId + "\",\"mapName\":\"Diagnostic Test Map\",\"waypoints\":[]}" }
        };
        
        var request = new UpdateUserDataRequest
        {
            Data = testMapData
        };
        
        PlayFabClientAPI.UpdateUserData(request,
            result => {
                Log("Test map entry created successfully");
                Log("Diagnostics complete! Check the results above.");
            },
            error => {
                Log($"Error creating test map entry: {error.ErrorMessage}");
                Log("Diagnostics complete with errors.");
            }
        );
    }
    
    private void Log(string message)
    {
        Debug.Log("[PlayFabDiagnostics] " + message);
        outputText.text += message + "\n";
        
        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}