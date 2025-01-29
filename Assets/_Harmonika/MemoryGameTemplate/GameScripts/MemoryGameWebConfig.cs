using UnityEngine;
using Harmonika.Tools;

[CreateAssetMenu(fileName = "MemoryGame Web Config", menuName = "Harmonika/ScriptableObjects/MemoryGame Web Config", order = 1)]
public class MemoryGameWebConfig : GameConfigScriptable
{
    [Space(5)]
    [Header("Configurable Variables")]
    public int memorizationTime = 2;
    public int gameTime = 20;

    [Space(5)]
    [Header("Cards")]
    public GameObject _cardPrefab;
    public Sprite cardBack;
    public Sprite[] cardPairs;

    [Space(5)]
    public Sprite userLogo;
    [Header("ProjectIdentity")]
    public string gameName = "My Memory Game";
    public Color primaryColor = "#2974DE".HexToColor();
    public Color secondaryColor = "#5429DE".HexToColor();
    public Color tertiaryColor = "#29D3DE".HexToColor();
    public Color neutralColor = "#FFFFFF".HexToColor();

}