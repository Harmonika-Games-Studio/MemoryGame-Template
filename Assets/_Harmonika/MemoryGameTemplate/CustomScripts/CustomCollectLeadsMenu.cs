using UnityEngine;
using UnityEngine.UI;

public class CustomCollectLeadsMenu : CollectLeadsMenu
{
    public Image background;

    public Button ContinueGameButton => _continueGameBtn;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        background.color = config.primaryColor;
        _continueGameBtn.image.color = config.secondaryColor;
    }
}
