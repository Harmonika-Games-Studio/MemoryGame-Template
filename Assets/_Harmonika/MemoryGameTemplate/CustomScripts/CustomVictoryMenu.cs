using UnityEngine;
using UnityEngine.UI;

public class CustomVictoryMenu : VictoryMenu
{
    public Image background;

    public Button BackButton => _backBtn;

    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        background.color = config.primaryColor;
        _backBtn.image.color = config.secondaryColor;
    }
}
