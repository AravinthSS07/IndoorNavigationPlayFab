using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using UnityEngine.UI;
using Unity.XR.CoreUtils; // Add this for XR Origin

public class SceneSetup : MonoBehaviour
{
    [Header("XR Components")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private XROrigin xrOrigin; // Changed from ARSessionOrigin
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARRaycastManager arRaycastManager;
    
    [Header("UI Components")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject mapCreationPanel;
    [SerializeField] private GameObject uploadMapPanel;
    [SerializeField] private GameObject navigationPanel;
    
    [Header("Manager Scripts")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ARManager arManager;
    [SerializeField] private PlayFabManager playFabManager;
    [SerializeField] private MapCreationManager mapCreationManager;
    [SerializeField] private NavigationManager navigationManager;
    
    public void ValidateSetup()
    {
        bool hasErrors = false;
        
        // Check XR components
        if (arSession == null) { Debug.LogError("Missing ARSession"); hasErrors = true; }
        if (xrOrigin == null) { Debug.LogError("Missing XROrigin"); hasErrors = true; }
        if (arPlaneManager == null) { Debug.LogError("Missing ARPlaneManager"); hasErrors = true; }
        if (arRaycastManager == null) { Debug.LogError("Missing ARRaycastManager"); hasErrors = true; }
        
        // Check UI components
        if (mainMenuPanel == null) { Debug.LogError("Missing Main Menu Panel"); hasErrors = true; }
        if (mapCreationPanel == null) { Debug.LogError("Missing Map Creation Panel"); hasErrors = true; }
        if (uploadMapPanel == null) { Debug.LogError("Missing Upload Map Panel"); hasErrors = true; }
        if (navigationPanel == null) { Debug.LogError("Missing Navigation Panel"); hasErrors = true; }
        
        // Check Manager scripts
        if (uiManager == null) { Debug.LogError("Missing UIManager"); hasErrors = true; }
        if (arManager == null) { Debug.LogError("Missing ARManager"); hasErrors = true; }
        if (playFabManager == null) { Debug.LogError("Missing PlayFabManager"); hasErrors = true; }
        if (mapCreationManager == null) { Debug.LogError("Missing MapCreationManager"); hasErrors = true; }
        if (navigationManager == null) { Debug.LogError("Missing NavigationManager"); hasErrors = true; }
        
        if (!hasErrors)
        {
            Debug.Log("Scene setup validated successfully!");
        }
    }
}