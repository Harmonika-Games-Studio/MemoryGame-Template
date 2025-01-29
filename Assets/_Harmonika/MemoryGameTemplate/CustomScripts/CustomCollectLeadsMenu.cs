using UnityEngine;
using UnityEngine.UI;

public class CustomizableCollectLeadsMenu : CollectLeadsMenu
{
    public Image backgroundImg;
    public Image backgroundCircleImg;
    public Image userLogo;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        backgroundImg.color = config.neutralColor;
        backgroundCircleImg.color = config.primaryColor;
        ContinueGameButton.image.color = config.secondaryColor;

    }
}
