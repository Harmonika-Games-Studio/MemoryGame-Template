using UnityEngine;
using UnityEngine.UI;

public class CustomCollectLeadsMenu : CollectLeadsMenu
{
    public Image background;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        background.color = config.primaryColor;
        ContinueGameButton.image.color = config.secondaryColor;
    }
}
