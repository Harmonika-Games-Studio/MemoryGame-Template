using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomizableStartMenu : StartMenu
{
    public Image backgroundImg;
    public Image backgroundCircleImg;
    public Image cardBack;
    public Image cardFront;
    public Image userLogo;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        backgroundImg.color = config.neutralColor;
        backgroundCircleImg.color = config.primaryColor;
        StartGameButton.image.color = config.secondaryColor;

        cardBack.sprite = config.cardBack;
        cardFront.sprite = config.cardPairs[Random.Range(0, config.cardPairs.Length)];
        userLogo.sprite = config.userLogo;
        TitleText = config.gameName;
    }
}
