using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Harmonika.Tools;
using System.Collections;

public class HapVidaCustomSelector : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private TMP_InputField _input;
    private CanvasGroup _inputCV;

    private void Awake()
    {
        _inputCV = _input.gameObject.GetComponentInParent<CanvasGroup>();
        _toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void Start()
    {
        StartCoroutine(SwitchCanvasGroupAnimated(_inputCV, Turn.on));
    }

    private void OnValueChanged(bool isOn)
    {
        StartCoroutine(SwitchCanvasGroupAnimated(_inputCV, isOn ? Turn.on : Turn.off));
        if (isOn) _input.text = "";
        else InvokeUtility.Invoke(() => _input.text = "nenhum", .1f);
    }


    IEnumerator SwitchCanvasGroupAnimated(CanvasGroup group, Turn turn, float duration = .1f)
    {
        float targetAlpha = (turn == Turn.on) ? 1 : 0;
        float startAlpha = group.alpha;
        float time = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
        while (time < duration)
        {
            time += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        group.alpha = targetAlpha;
        if (turn == Turn.on)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }
}
