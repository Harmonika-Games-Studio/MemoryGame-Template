using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomStartMenu : StartMenu
{
    public Image background;
    public Image cardBack;
    public Image cardFront;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        cardBack.sprite = config.cardBack;
        cardFront.sprite = config.cardPairs[Random.Range(0, config.cardPairs.Length)];
        background.color = config.primaryColor;
        StartGameButton.image.color = config.secondaryColor;
        TitleText = config.gameName;
    }
}
