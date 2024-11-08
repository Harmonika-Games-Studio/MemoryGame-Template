using UnityEngine;
using Harmonika.Tools;

[CreateAssetMenu(fileName = "MemoryGame Config", menuName = "Harmonika/ScriptableObjects/MemoryGame Config", order = 1)]
public class MemoryGameConfig : GameConfigScriptable
{
    [Space(5)]
    [Header("Configurable Variables")]
    public int memorizationTime = 3;
    public int gameTimer = 20;

    [Space(5)]
    [Header("Cards")]
    public Sprite cardBack;
    public Sprite[] cardPairs;

    [Space(5)]
    [Header("Visual Identity")]
    public Sprite userLogo;
    public Color primaryClr;
    public Color secondaryClr;
    public Color fontClr;

    [Space(5)]
    [Header("Game Identity")]
    public string gameName;

    /*[Space(5)]
    [Header("Leads InputFields")]
    public LeadConfig[] leads;*/ //Not yet implemented

    //[Space(5)]
    //[Header("Audio")]
    //public AudioClip backgroundMusic;
    //public AudioClip cardClick;
    //public AudioClip victory;
    //public AudioClip lose;

    //[Space(5)]
    //[Header("MainMenu")]
    //public Sprite userLogo;
    //public Sprite mainBackground;
    //public string projectTitle;
    //public Sprite gameImage;
    //public Sprite startButtonImg;
    //public Color titleClr;
    //public Color startButtonClr;

    //[Space(5)]
    //[Header("LeadsMenu")]
    //public Sprite leadsBackground;
    //public Sprite continueButtonImg;
    //public string leadsText;
    //public Color leadsTextClr;
    //public Color leadsButtonClr;

    //[Space(5)]
    //[Header("FinalMenu")]
    //public string victoryMainText;
    //public string victorySubText;
    //public Sprite victoryEmoji;
    //public Color victoryFontColor;
    //public string participationMainText;
    //public string participationSubText;
    //public Sprite participationEmoji;
    //public Color participationFontColor;
    //public string loseMainText;
    //public string loseSubText;
    //public Sprite loseEmoji;
    //public Color loseFontColor;
    //public Sprite finalizeButtonImg;
}