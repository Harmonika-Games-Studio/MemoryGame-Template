using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ParticipationMenu : MonoBehaviour
{
    [SerializeField] private Button _backToMainMenu;
    [SerializeField] private TMP_Text _participationText;

    public string VictoryText
    {
        get => _participationText.text;
        set
        {
            _participationText.text = value;
        }
    }

    public void AddBackToMainMenuButtonListener(UnityAction action)
    {
        _backToMainMenu.onClick.AddListener(action);
    }

    public void ChangeVisualIdentity(Color a)
    {
        _participationText.color = a;
    }
}
