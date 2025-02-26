using UnityEngine;
using System;

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;
    
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
    
    public static SerializableVector3 FromVector3(Vector3 vector)
    {
        return new SerializableVector3(vector);
    }
}

[Serializable]
public class SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
    
    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }
    
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
    
    public static SerializableQuaternion FromQuaternion(Quaternion quaternion)
    {
        return new SerializableQuaternion(quaternion);
    }
}