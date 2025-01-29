using UnityEngine;
using UnityEngine.UI;

public class CustomizableParticipationMenu : ParticipationMenu
{
    public Image backgroundImg;
    public Image backgroundCircleImg;
    public Image userLogoImg;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        backgroundImg.color = config.primaryColor;
        backgroundCircleImg.color = config.neutralColor;
        BackButton.image.color = config.secondaryColor;

        userLogoImg.sprite = config.userLogo;
    }
}
