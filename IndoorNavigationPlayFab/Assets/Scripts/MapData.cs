using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaypointData
{
    // For serialization
    public SerializableVector3 serializedPosition;
    public SerializableQuaternion serializedRotation;
    
    // Non-serialized fields for runtime use
    [NonSerialized]
    public Vector3 position;
    [NonSerialized]
    public Quaternion rotation;
    
    public bool isSource;
    public bool isDestination;
    public string label;
    
    // Default constructor for serialization
    public WaypointData()
    {
    }
    
    public WaypointData(Vector3 position, Quaternion rotation, 
                        bool isSource = false, bool isDestination = false, 
                        string label = "")
    {
        this.position = position;
        this.rotation = rotation;
        this.serializedPosition = new SerializableVector3(position);
        this.serializedRotation = new SerializableQuaternion(rotation);
        this.isSource = isSource;
        this.isDestination = isDestination;
        this.label = label;
    }
    
    // Method to prepare for serialization
    public void PrepareForSerialization()
    {
        serializedPosition = new SerializableVector3(position);
        serializedRotation = new SerializableQuaternion(rotation);
    }
    
    // Method to restore after deserialization
    public void RestoreFromSerialization()
    {
        position = serializedPosition?.ToVector3() ?? Vector3.zero;
        rotation = serializedRotation?.ToQuaternion() ?? Quaternion.identity;
    }
}

[Serializable]
public class MapData
{
    public string mapId;
    public string mapName;
    public string creatorId;
    public DateTime creationDate;
    public List<WaypointData> waypoints = new List<WaypointData>();
    public int sourceWaypointIndex = -1;
    public int destinationWaypointIndex = -1;
    
    // Prepare the entire map for serialization
    public void PrepareForSerialization()
    {
        foreach (var waypoint in waypoints)
        {
            waypoint.PrepareForSerialization();
        }
    }
    
    // Restore the entire map after deserialization
    public void RestoreFromSerialization()
    {
        foreach (var waypoint in waypoints)
        {
            waypoint.RestoreFromSerialization();
        }
    }
}