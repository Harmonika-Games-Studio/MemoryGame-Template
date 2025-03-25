using TMPro;
using UnityEngine;

public class TimeTextHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;

    public void ShowTime(int time)
    {
        int minutes = time / 60;
        int seconds = time % 60;

        timeText.text = $"Você ganhou em {string.Format("{0:00}:{1:00}", minutes, seconds)}";
    }

}
