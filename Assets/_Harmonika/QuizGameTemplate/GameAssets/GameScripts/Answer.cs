using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static System.TimeZoneInfo;
using System.Collections;

public class Answer : MonoBehaviour
{
    [HideInInspector] public Button button;
    [HideInInspector] public TMP_Text ansText;
    [HideInInspector] public bool isCorrect = false;
    [HideInInspector] public QuizGame quizGame;

    private void Awake()
    {
        ansText = GetComponentInChildren<TMP_Text>();
        button = GetComponent<Button>();

        button.onClick.AddListener(() => Clicked());
    }

    void Clicked()
    {
        if (isCorrect) ChangeColor(Color.green);
        else ChangeColor(Color.red);

        quizGame.ClickedOnAnswer(this);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }

    public void ChangeColor(Color c, float t = .1f)
    {
        StartCoroutine(ChangeButtonColor(c, t));
    }

    private IEnumerator ChangeButtonColor(Color targetColor, float transitionTime)
    {
        ColorBlock cb = button.colors;
        Color currentColor = cb.normalColor; // Cor atual do botão
        float elapsedTime = 0f;

        while (elapsedTime < transitionTime)
        {
            float t = elapsedTime / transitionTime;
            Color newColor = Color.Lerp(currentColor, targetColor, t);

            cb.normalColor = newColor;
            cb.pressedColor = newColor;
            cb.selectedColor = newColor;
            cb.disabledColor = newColor;
            button.colors = cb;

            elapsedTime += Time.deltaTime;
            yield return null; // Espera um frame
        }

        // Garantir que a cor final seja a cor alvo
        cb.normalColor = targetColor;
        cb.pressedColor = targetColor;
        cb.selectedColor = targetColor;
        cb.disabledColor = targetColor;
        button.colors = cb;
    }
}