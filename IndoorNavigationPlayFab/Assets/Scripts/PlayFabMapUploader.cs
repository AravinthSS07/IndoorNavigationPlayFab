using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;

public class PlayFabMapUploader : MonoBehaviour
{
    public static PlayFabMapUploader Instance;
    public Dropdown mapDropdown; // UI Dropdown to show available maps

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UploadMap(string mapName, List<Vector3> waypoints)
    {
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogError("Map name cannot be empty!");
            return;
        }

        // Convert waypoints into a JSON string
        MapData mapData = new MapData { waypoints = waypoints };
        string jsonData = JsonConvert.SerializeObject(mapData);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { mapName, jsonData }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnDataUploadSuccess, OnDataUploadFailure);
    }

    private void OnDataUploadSuccess(UpdateUserDataResult result)
    {
        Debug.Log("Map uploaded successfully!");
        FetchAvailableMaps(); // Refresh the map list after uploading
    }

    private void OnDataUploadFailure(PlayFabError error)
    {
        Debug.LogError("Failed to upload map: " + error.GenerateErrorReport());
    }

    public void DownloadMap(string mapName, Action<List<Vector3>> onSuccess, Action<string> onFailure)
    {
        var request = new GetUserDataRequest();

        PlayFabClientAPI.GetUserData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey(mapName))
            {
                string jsonData = result.Data[mapName].Value;
                MapData mapData = JsonConvert.DeserializeObject<MapData>(jsonData);
                onSuccess?.Invoke(mapData.waypoints);
            }
            else
            {
                onFailure?.Invoke("Map not found!");
            }
        }, error =>
        {
            onFailure?.Invoke("Failed to download map: " + error.GenerateErrorReport());
        });
    }

    public void FetchAvailableMaps()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            if (result.Data != null)
            {
                List<string> mapNames = new List<string>(result.Data.Keys);
                PopulateDropdown(mapNames);
            }
        }, error =>
        {
            Debug.LogError("Failed to fetch map list: " + error.GenerateErrorReport());
        });
    }

    private void PopulateDropdown(List<string> mapNames)
    {
        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(mapNames);
    }

    public void OnMapSelected()
    {
        string selectedMap = mapDropdown.options[mapDropdown.value].text;
        DownloadMap(selectedMap, waypoints =>
        {
            Debug.Log("Map loaded successfully!");
            // Handle loading waypoints into AR navigation system
        }, error => Debug.LogError(error));
    }
}

[Serializable]
public class MapData
{
    public List<Vector3> waypoints;
}

[System.Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
