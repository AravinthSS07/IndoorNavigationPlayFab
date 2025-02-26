using UnityEngine;

public class DirectionIndicator : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private Color arrowColor = new Color(0.2f, 0.8f, 0.2f);
    
    private Material arrowMaterial;
    private float initialScale;
    
    void Start()
    {
        // Create material instance for color control
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            arrowMaterial = new Material(renderer.material);
            arrowMaterial.color = arrowColor;
            renderer.material = arrowMaterial;
        }
        
        initialScale = transform.localScale.y;
    }
    
    void Update()
    {
        // Make the arrow pulse to draw attention
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount + 1.0f;
        
        // Only pulse the scale in the "up" direction
        Vector3 scale = transform.localScale;
        scale.y = initialScale * pulse;
        transform.localScale = scale;
    }
}