using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneIndicator : MonoBehaviour
{
    public ARRaycastManager RaycastManager;
    public GameObject IndicatorPrefab;
    private GameObject indicator;

    void Start()
    {
        indicator = Instantiate(IndicatorPrefab);
        indicator.SetActive(false);
    }

    void Update()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        RaycastManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), hits, TrackableType.Planes);

        if (hits.Count > 0)
        {
            transform.position = hits[0].pose.position;
            transform.rotation = hits[0].pose.rotation;

            if (!indicator.activeInHierarchy)
            {
                indicator.SetActive(true);
            }
        }
        else
        {
            indicator.SetActive(false);
        }
    }
}