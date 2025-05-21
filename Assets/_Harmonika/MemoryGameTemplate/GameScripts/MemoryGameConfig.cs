using UnityEngine;
using Harmonika.Tools;

[CreateAssetMenu(fileName = "MemoryGame Config", menuName = "Harmonika/ScriptableObjects/MemoryGame Config", order = 1)]
public class MemoryGameConfig : GameConfigScriptable
{
    [Space(5)]
    [Header("Configurable Variables")]
    public int memorizationTime = 3;
    public int gameTime =30;

    [Space(5)]
    [Header("Cards")]
    public GameObject _cardPrefab;
    public Sprite cardBack;
    public Sprite[] cardBacksDraw;
    public Sprite[] cardPairs;
}