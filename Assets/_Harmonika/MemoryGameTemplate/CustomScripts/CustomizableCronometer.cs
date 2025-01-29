using UnityEngine.UI;

public class CustomizableCronometer : Cronometer
{
    public Image image;
    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        image.color = config.tertiaryColor;
    }
}
