using UnityEngine;
using UnityEngine.UI;

public class CustomVictoryMenu : VictoryMenu
{
    public Image background;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        background.color = config.primaryColor;
        BackButton.image.color = config.secondaryColor;
    }
}
