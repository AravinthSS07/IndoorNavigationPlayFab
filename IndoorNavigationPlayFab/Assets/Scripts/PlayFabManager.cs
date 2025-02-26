using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TMPro;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public string PlayFabId { get; private set; }
    
    private const string TITLE_ID = "4C8F1"; // Replace with your PlayFab Title ID

    public TMP_Text statusText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PlayFabSettings.TitleId = TITLE_ID;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoginAnonymously();
    }
    
    public void LoginAnonymously(System.Action<bool> onCompleteCallback = null)
    {
        Debug.Log("Attempting PlayFab login...");
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(request, 
            result => {
                IsAuthenticated = true;
                PlayFabId = result.PlayFabId;
                Debug.Log("Logged into PlayFab: " + PlayFabId);
                onCompleteCallback?.Invoke(true);
            }, 
            error => {
                Debug.LogError("PlayFab login failed: " + error.ErrorMessage);
                IsAuthenticated = false;
                onCompleteCallback?.Invoke(false);
            });
    }
    
    private void OnLoginSuccess(LoginResult result)
    {
        IsAuthenticated = true;
        PlayFabId = result.PlayFabId;
        Debug.Log("Logged into PlayFab: " + PlayFabId);
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("PlayFab login failed: " + error.ErrorMessage);
        IsAuthenticated = false;
    }

    public void UploadMap(MapData mapData, Action<bool, string> callback)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Must be authenticated to upload maps");
            callback?.Invoke(false, "Not authenticated");
            return;
        }

        // Prepare map data for serialization
        mapData.PrepareForSerialization();

        // Generate a unique map ID if one doesn't exist
        if (string.IsNullOrEmpty(mapData.mapId))
        {
            mapData.mapId = System.Guid.NewGuid().ToString();
        }

        // Set creation metadata
        mapData.creatorId = PlayFabId;
        mapData.creationDate = System.DateTime.UtcNow;

        string mapJson = JsonUtility.ToJson(mapData);
        Debug.Log($"Map JSON size: {mapJson.Length} characters");

        // Upload map data to user data
        var mapDataRequest = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "Map_" + mapData.mapId, mapJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(mapDataRequest,
            result => {
                Debug.Log("Map uploaded successfully: " + mapData.mapId);

                // Now create/update the map catalog directly in user data
                UpdateMapCatalogDirectly(mapData, callback);
            },
            error => {
                Debug.LogError("Error uploading map: " + error.ErrorMessage);
                callback?.Invoke(false, error.ErrorMessage);
            }
        );
    }

    // Also update the loading method:
    private void ProcessMapData(string mapJson, Action<MapData> callback)
    {
        try
        {
            MapData mapData = JsonUtility.FromJson<MapData>(mapJson);

            // Restore the Vector3 and Quaternion values from serialized data
            mapData.RestoreFromSerialization();

            callback(mapData);
        }
        catch (Exception e)
        {
            Debug.LogError("Error processing map data: " + e.Message);
            callback(null);
        }
    }
    
    private void UpdateMapCatalog(MapData mapData, Action<bool, string> callback)
    {
        Debug.Log($"Updating map catalog for map: {mapData.mapName} with ID {mapData.mapId}");

        // Create a simple catalog entry for this map
        var mapEntry = new Dictionary<string, string>
        {
            { "mapId", mapData.mapId },
            { "mapName", mapData.mapName },
            { "creatorId", PlayFabId },
            { "creationDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
        };

        string mapCatalogEntryJson = JsonUtility.ToJson(new MapCatalogEntry
        {
            mapId = mapData.mapId,
            mapName = mapData.mapName,
            creatorId = PlayFabId,
            creationDate = DateTime.UtcNow
        });

        Debug.Log($"Map catalog entry JSON: {mapCatalogEntryJson}");

        // Use ExecuteCloudScript to update the map catalog
        var request = new ExecuteCloudScriptRequest
        {
            FunctionName = "addMapToCatalog",
            FunctionParameter = new { 
                mapId = mapData.mapId,
                mapName = mapData.mapName,
                creationDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request,
            result => {
                if (result.Error != null)
                {
                    Debug.LogError($"Error updating map catalog: {result.Error.Message}");
                    callback?.Invoke(false, result.Error.Message);
                    return;
                }

                Debug.Log("Map catalog updated successfully");
                Debug.Log($"Cloud Script response: {result.FunctionResult?.ToString() ?? "null"}");
                callback?.Invoke(true, mapData.mapId);
            },
            error => {
                Debug.LogError($"Cloud Script error: {error.ErrorMessage}");
                // The map is still uploaded, we just couldn't update the catalog
                callback?.Invoke(true, mapData.mapId);
            }
        );
    }
    
    public void GetAllAvailableMaps(Action<List<MapCatalogEntry>> callback)
    {
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest
        {
            Keys = new List<string> { "MapCatalog" }
        },
        result => {
            if (result.Data != null && result.Data.ContainsKey("MapCatalog") && !string.IsNullOrEmpty(result.Data["MapCatalog"]))
            {
                var catalog = JsonConvert.DeserializeObject<List<MapCatalogEntry>>(result.Data["MapCatalog"]);
                callback(catalog);
            }
            else
            {
                callback(new List<MapCatalogEntry>());
            }
        },
        error => {
            Debug.LogError("Failed to get map catalog: " + error.ErrorMessage);
            callback(new List<MapCatalogEntry>());
        });
    }
    
    public void LoadMap(string mapId, string creatorId, Action<MapData> callback)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = creatorId,
            Keys = new List<string> { "Map_" + mapId }
        },
        result => {
            if (result.Data != null && result.Data.ContainsKey("Map_" + mapId))
            {
                try
                {
                    var mapData = JsonConvert.DeserializeObject<MapData>(result.Data["Map_" + mapId].Value);
                    callback(mapData);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to deserialize map data: " + ex.Message);
                    callback(null);
                }
            }
            else
            {
                Debug.LogError("Map not found: " + mapId);
                callback(null);
            }
        },
        error => {
            Debug.LogError("Failed to load map: " + error.ErrorMessage);
            callback(null);
        });
    }

    public void TestPlayFabUpload()
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Not authenticated with PlayFab");
            UIManager.Instance.ShowNotification("Not authenticated with PlayFab. Please restart the app.");
            return;
        }

        // Format the current date time in the specified format (UTC)
        string currentDateTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Use your provided login as user identifier
        string userLogin = "AravinthSS07";

        // Create a simple test payload with the standardized format
        var testData = new Dictionary<string, string>
        {
            { "testTimestamp", currentDateTime },
            { "testUser", userLogin },
            { "testDevice", SystemInfo.deviceModel }
        };

        Debug.Log($"Testing PlayFab connection with timestamp: {currentDateTime}");

        // Create the PlayFab request
        var request = new PlayFab.ClientModels.UpdateUserDataRequest
        {
            Data = testData
        };

        // Show status to user
        UIManager.Instance.ShowNotification("Testing PlayFab connection...");

        // Send the request to PlayFab
        PlayFabClientAPI.UpdateUserData(request, 
            result => {
                Debug.Log("PlayFab test successful! Data uploaded with timestamp: " + currentDateTime);
                UIManager.Instance.ShowNotification("PlayFab connection verified successfully!");

                // Verify we can read the data back
                VerifyPlayFabDataRetrieval();
            },
            error => {
                Debug.LogError($"PlayFab test failed: {error.ErrorMessage}");
                UIManager.Instance.ShowNotification($"PlayFab test failed: {error.ErrorMessage}");
            }
        );
    }

    public void TestMapRetrieval()
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Must be authenticated to retrieve maps");
            UIManager.Instance.ShowNotification("Not authenticated with PlayFab");
            return;
        }

        Debug.Log("Testing direct map retrieval...");
        UIManager.Instance.ShowNotification("Testing map retrieval...");

        // Try to directly access user data for maps
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request,
            result => {
                Debug.Log($"GetUserData returned {result.Data.Count} items");

                int mapCount = 0;
                foreach (var item in result.Data)
                {
                    if (item.Key.StartsWith("Map_"))
                    {
                        mapCount++;
                        Debug.Log($"Found map: {item.Key}");
                    }
                }

                if (mapCount > 0)
                {
                    Debug.Log($"Found {mapCount} maps in user data");
                    UIManager.Instance.ShowNotification($"Found {mapCount} maps directly in user data");
                }
                else
                {
                    Debug.Log("No maps found in user data");
                    UIManager.Instance.ShowNotification("No maps found directly in user data");
                }

                // Now try to get the map catalog
                GetMapCatalog(catalog => {
                    Debug.Log($"Map catalog has {catalog.Count} entries");
                    UIManager.Instance.ShowNotification($"Map catalog has {catalog.Count} entries");
                });
            },
            error => {
                Debug.LogError($"Error retrieving user data: {error.ErrorMessage}");
                UIManager.Instance.ShowNotification($"Error: {error.ErrorMessage}");
            }
        );
    }

    /// <summary>
    /// Verifies data can be read back from PlayFab
    /// </summary>
    private void VerifyPlayFabDataRetrieval()
    {
        var request = new PlayFab.ClientModels.GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request,
            result => {
                if (result.Data != null && result.Data.ContainsKey("testTimestamp"))
                {
                    string retrievedTime = result.Data["testTimestamp"].Value;
                    Debug.Log($"Retrieved test data from PlayFab: {retrievedTime}");
                    UIManager.Instance.ShowNotification($"Data retrieved successfully: {retrievedTime}");
                }
                else
                {
                    Debug.LogWarning("Test data was not found in PlayFab.");
                    UIManager.Instance.ShowNotification("Test data was uploaded but couldn't be retrieved.");
                }
            },
            error => {
                Debug.LogError($"Failed to retrieve test data: {error.ErrorMessage}");
                UIManager.Instance.ShowNotification("Failed to retrieve test data.");
            }
        );
    }

    public void GetMapCatalog(Action<List<MapCatalogEntry>> callback)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("Must be authenticated to get map catalog");
            callback?.Invoke(new List<MapCatalogEntry>());
            return;
        }
        
        Debug.Log("Retrieving map catalog directly from user data...");
        
        var request = new GetUserDataRequest
        {
            Keys = new List<string> { "MapCatalog" }
        };
        
        PlayFabClientAPI.GetUserData(request,
            result => {
                List<MapCatalogEntry> catalog = new List<MapCatalogEntry>();
                
                if (result.Data.ContainsKey("MapCatalog"))
                {
                    try
                    {
                        string catalogJson = result.Data["MapCatalog"].Value;
                        Debug.Log($"Retrieved catalog JSON: {catalogJson}");
                        
                        MapCatalogArray catalogArray = JsonUtility.FromJson<MapCatalogArray>(catalogJson);
                        if (catalogArray != null && catalogArray.maps != null)
                        {
                            catalog.AddRange(catalogArray.maps);
                            Debug.Log($"Successfully parsed catalog with {catalog.Count} entries");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing map catalog: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning("No MapCatalog entry found in user data");
                }
                
                if (catalog.Count == 0)
                {
                    Debug.Log("No maps found in catalog, falling back to direct map search");
                    GetMapCatalogFallback(callback);
                }
                else
                {
                    callback?.Invoke(catalog);
                }
            },
            error => {
                Debug.LogError($"Error retrieving map catalog: {error.ErrorMessage}");
                GetMapCatalogFallback(callback);
            }
        );
    }

    // Fallback method if CloudScript fails
    private void GetMapCatalogFallback(Action<List<MapCatalogEntry>> callback)
    {
        Debug.Log("Using fallback method to get map catalog");

        // Try to get maps directly from user data
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request,
            result => {
                var catalog = new List<MapCatalogEntry>();

                foreach (var item in result.Data)
                {
                    // Look for map data entries (they start with Map_)
                    if (item.Key.StartsWith("Map_"))
                    {
                        try
                        {
                            string mapId = item.Key.Substring(4); // Remove "Map_" prefix

                            // Parse the map data to get basic info
                            MapData mapData = JsonUtility.FromJson<MapData>(item.Value.Value);

                            // Create a catalog entry
                            var entry = new MapCatalogEntry
                            {
                                mapId = mapId,
                                mapName = mapData.mapName,
                                creatorId = mapData.creatorId,
                                creationDate = mapData.creationDate
                            };

                            catalog.Add(entry);
                            Debug.Log($"Added map to catalog: {entry.mapName}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"Error parsing map data for {item.Key}: {e.Message}");
                        }
                    }
                }

                Debug.Log($"Retrieved {catalog.Count} maps from user data");
                callback?.Invoke(catalog);
            },
            error => {
                Debug.LogError($"Error retrieving user data: {error.ErrorMessage}");
                callback?.Invoke(new List<MapCatalogEntry>());
            }
        );
    }

    private void UpdateMapCatalogDirectly(MapData mapData, Action<bool, string> callback)
    {
        Debug.Log($"Updating map catalog directly for map: {mapData.mapName} with ID {mapData.mapId}");

        // First retrieve the current catalog
        var getCatalogRequest = new GetUserDataRequest
        {
            Keys = new List<string> { "MapCatalog" }
        };

        PlayFabClientAPI.GetUserData(getCatalogRequest,
            result => {
                List<MapCatalogEntry> catalog = new List<MapCatalogEntry>();

                // Parse existing catalog if it exists
                if (result.Data.ContainsKey("MapCatalog"))
                {
                    try
                    {
                        // Try to parse existing catalog
                        string catalogJson = result.Data["MapCatalog"].Value;
                        MapCatalogArray catalogArray = JsonUtility.FromJson<MapCatalogArray>(catalogJson);
                        if (catalogArray != null && catalogArray.maps != null)
                        {
                            catalog.AddRange(catalogArray.maps);
                            Debug.Log($"Retrieved existing map catalog with {catalog.Count} entries");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error parsing existing catalog: {e.Message}");
                        // Continue with empty catalog
                    }
                }

                // Check if this map is already in the catalog
                bool mapExists = false;
                for (int i = 0; i < catalog.Count; i++)
                {
                    if (catalog[i].mapId == mapData.mapId)
                    {
                        // Update existing entry
                        catalog[i].mapName = mapData.mapName;
                        catalog[i].creationDate = mapData.creationDate;
                        mapExists = true;
                        break;
                    }
                }

                // Add new entry if map doesn't exist in catalog
                if (!mapExists)
                {
                    MapCatalogEntry newEntry = new MapCatalogEntry
                    {
                        mapId = mapData.mapId,
                        mapName = mapData.mapName,
                        creatorId = PlayFabId,
                        creationDate = mapData.creationDate
                    };
                    catalog.Add(newEntry);
                }

                // Serialize the catalog back to JSON
                MapCatalogArray catalogToSave = new MapCatalogArray { maps = catalog.ToArray() };
                string updatedCatalogJson = JsonUtility.ToJson(catalogToSave);

                Debug.Log($"Saving map catalog with {catalog.Count} entries");

                // Save the updated catalog
                var updateRequest = new UpdateUserDataRequest
                {
                    Data = new Dictionary<string, string>
                    {
                        { "MapCatalog", updatedCatalogJson }
                    }
                };

                PlayFabClientAPI.UpdateUserData(updateRequest,
                    updateResult => {
                        Debug.Log("Map catalog updated successfully");
                        callback?.Invoke(true, mapData.mapId);
                    },
                    updateError => {
                        Debug.LogError($"Error updating map catalog: {updateError.ErrorMessage}");
                        // Still return success since the map was uploaded
                        callback?.Invoke(true, mapData.mapId);
                    }
                );
            },
            error => {
                Debug.LogError($"Error retrieving map catalog: {error.ErrorMessage}");
                // Still return success since the map was uploaded
                callback?.Invoke(true, mapData.mapId);
            }
        );
    }

    // Add this class to help with serialization
    [System.Serializable]
    public class MapCatalogArray
    {
        public MapCatalogEntry[] maps;
    }
}

[Serializable]
public class MapCatalogEntry
{
    public string mapId;
    public string mapName;
    public string creatorId;
    public DateTime creationDate;
    public DateTime lastUpdated;
}

[Serializable]
internal class MapCatalogResponse
{
    public bool success;
    public MapCatalogEntry[] catalog;
    public int personalMapCount;
    public int sharedMapCount;
}