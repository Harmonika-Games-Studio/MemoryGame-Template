using UnityEngine.UI;

public class CustomCronometer : Cronometer
{
    public Image image;
    public void ChangeVisualIdentity(MemoryGameWebConfig config)
    {
        image.color = config.secondaryColor;
    }
}
