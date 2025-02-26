using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

public class ARManager : MonoBehaviour
{
    public static ARManager Instance { get; private set; }
    
    [SerializeField] private ARSession arSession;
    [SerializeField] private XROrigin xrOrigin; // Changed from ARSessionOrigin
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARAnchorManager arAnchorManager;
    
    // Update the camera property to access it through XROrigin
    public Camera ARCamera => xrOrigin.Camera;
    public ARAnchorManager AnchorManager => arAnchorManager;
    
    private bool isSessionStarted = false;
    
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
        
        if (arSession == null) arSession = FindObjectOfType<ARSession>();
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();
        if (arPlaneManager == null) arPlaneManager = FindObjectOfType<ARPlaneManager>();
        if (arRaycastManager == null) arRaycastManager = FindObjectOfType<ARRaycastManager>();
        if (arAnchorManager == null) arAnchorManager = FindObjectOfType<ARAnchorManager>();
    }
    
    public void StartARSession()
    {
        if (!isSessionStarted)
        {
            arSession.enabled = true;
            arPlaneManager.enabled = true;
            arAnchorManager.enabled = true;
            isSessionStarted = true;
        }
    }
    
    public void StopARSession()
    {
        if (isSessionStarted)
        {
            arSession.Reset();
            arSession.enabled = false;
            arPlaneManager.enabled = false;
            arAnchorManager.enabled = false;
            isSessionStarted = false;
        }
    }
    
    public bool TryGetPlacementPose(out Pose pose)
    {
        pose = Pose.identity;
        
        // Try to raycast against a plane
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        
        if (arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            pose = hits[0].pose;
            return true;
        }
        
        return false;
    }
    
    // Create an AR anchor at the specified pose 
    public ARAnchor CreateAnchor(Pose pose)
    {
        ARAnchor anchor = null;
        
        // Try to create using anchor manager first
        if (arAnchorManager != null)
        {
            anchor = arAnchorManager.AddAnchor(pose);
        }
        
        // Fallback to adding component directly if anchor manager failed
        if (anchor == null)
        {
            GameObject anchorObject = new GameObject("AR Anchor");
            anchorObject.transform.position = pose.position;
            anchorObject.transform.rotation = pose.rotation;
            anchor = anchorObject.AddComponent<ARAnchor>();
        }
        
        return anchor;
    }
    
    public void TogglePlaneVisibility(bool visible)
    {
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }
    
    // Call this when starting map creation
    public void SetupForMapCreation()
    {
        TogglePlaneVisibility(true);
        StartARSession();
    }
    
    // Call this when starting navigation
    public void SetupForNavigation()
    {
        TogglePlaneVisibility(false);
        StartARSession();
    }
}