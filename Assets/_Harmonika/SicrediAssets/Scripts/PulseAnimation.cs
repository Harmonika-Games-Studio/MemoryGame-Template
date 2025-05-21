using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PulseAnimation : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Range(0.5f, 1.5f)] 
    public float minScale = 0.5f;

    [Range(0.5f, 1.5f)]
    public float maxScale = 1.5f;

    [Tooltip("Time in seconds for one complete pulse cycle (expand and contract).")]
    public float pulseDuration = 1f;

    private Vector3 initialScale;
    private float timer;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime / pulseDuration;

        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(timer * Mathf.PI * 2) + 1) / 2);

        transform.localScale = initialScale * scale;

        if (timer > 1f) timer = 0f;
    }
}
