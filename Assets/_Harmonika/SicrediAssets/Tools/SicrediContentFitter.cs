using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class SicrediContentFitter : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private int minSize = 100;
    [SerializeField] private int offset = 0;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (textComponent == null)
        {
            if(!TryGetComponent<TMP_Text>(out textComponent))
            {
                Debug.LogError("ContentFitter: TMP_Text component not found. Please ensure that a TMP_Text component is attached to this GameObject or assign it in the Inspector.");
            }
        }
    }

    private void Start()
    {
        if (textComponent != null)
            AdjustWidth();
    }

    private void AdjustWidth()
    {
        Vector2 preferredValues = textComponent.GetPreferredValues();
        float newWidth = preferredValues.x + offset;
        newWidth = Mathf.Max(newWidth, minSize);

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}
