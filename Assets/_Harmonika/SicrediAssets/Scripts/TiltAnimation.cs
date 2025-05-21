using UnityEngine;

public class TiltAnimation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Range(-720f, 0)]
    public float minRotation = -15f;

    [Range(0, 720)]
    public float maxRotation = 15f;

    [Tooltip("Tempo total em segundos para completar um ciclo de inclinação.")]
    public float tiltDuration = .25f;   

    private float _currentTime;       
    private bool _movingForward = true;

    void Update()
    {
        if (tiltDuration <= 0f) return;

        _currentTime += (_movingForward ? 1 : -1) * (Time.deltaTime / (tiltDuration / 2f));

        _currentTime = Mathf.Clamp01(_currentTime);

        float currentRotation = Mathf.Lerp(minRotation, maxRotation, EaseInOut(_currentTime));

        transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);

        if (_currentTime >= 1f || _currentTime <= 0f)
        {
            _movingForward = !_movingForward;
        }
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
