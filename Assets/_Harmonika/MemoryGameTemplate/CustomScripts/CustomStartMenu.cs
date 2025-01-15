using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomStartMenu : StartMenu
{
    public Image background;
    public Image cardBack;
    public Image cardFront;

    public Button StartGameButton => _startGameBtn;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        cardBack.sprite = config.cardBack;
        cardFront.sprite = config.cardPairs[Random.Range(0, config.cardPairs.Length)];
        background.color = config.primaryColor;
        _startGameBtn.image.color = config.secondaryColor;
        _titleText.text = config.gameName;
    }
}
