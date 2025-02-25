using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUpload : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void testUpload(){
        PlayFabMapUploader.Instance.UploadMap("TestMap", new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0) });
    }
}
