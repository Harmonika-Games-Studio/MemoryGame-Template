using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button _startGame;
    [SerializeField] private TMP_Text _titleText;

    public string TitleText
    {
        get => _titleText.text;
        set
        {
            Debug.Log(value);
            _titleText.text = value;
        }
    }

    public void AddStartGameButtonListener(UnityAction action)
    {
        _startGame.onClick.AddListener(action);
    }

    public void ChangeVisualIdentity(Color titleColor)
    {
        _titleText.color = titleColor;
    }
}
