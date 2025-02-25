using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PlayFabMapUploader : MonoBehaviour
{
    public static PlayFabMapUploader Instance;

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
    }

    private void OnDataUploadFailure(PlayFabError error)
    {
        Debug.LogError("Failed to upload map: " + error.GenerateErrorReport());
    }
}

[Serializable]
public class MapData
{
    public List<Vector3> waypoints;
}
