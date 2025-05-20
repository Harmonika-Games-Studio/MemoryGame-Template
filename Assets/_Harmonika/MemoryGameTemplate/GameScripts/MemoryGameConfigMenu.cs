using System.Linq;
using TMPro;
using UnityEngine;

public class MemoryGameConfigMenu : ConfigMenu
{
    [Header("Game Config")]
    [SerializeField] private MemoryGame _memoryGame;

    [SerializeField] private TMP_InputField _inputGameTime;
    [SerializeField] private TMP_InputField _inputMemorizationTime;
    [SerializeField] private TMP_InputField _inputRevealsToWin;

    protected override void Awake()
    {
        base.Awake();
        SetInitialValues();
        _inputGameTime.onSubmit.AddListener(OnGameTimeValueChanged);
        _inputMemorizationTime.onSubmit.AddListener(OnMemorizationTimeValueChanged);
        _inputRevealsToWin.onSubmit.AddListener(OnRevealsToWinValueChanged);
    }

    protected override void Start()
    {
        base.Start();
        UpdateInputValues();
    }

    private void SetInitialValues()
    {
        if (!PlayerPrefs.HasKey("GameTime"))
        {
            PlayerPrefs.SetInt("GameTime", _memoryGame.Config.gameTime);
            PlayerPrefs.Save();
        }

        if (!PlayerPrefs.HasKey("MemorizationTime"))
        {
            PlayerPrefs.SetInt("MemorizationTime", _memoryGame.Config.memorizationTime);
            PlayerPrefs.Save();
        }

        if (!PlayerPrefs.HasKey("RevealsToWin"))
        {
            PlayerPrefs.SetInt("RevealsToWin", 3);
            PlayerPrefs.Save();
        }
    }

    private void OnGameTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 10)
        {
            value = "10";
        }
        else if (int.Parse(value) > 999)
        {
            value = "999";
        }
        _inputGameTime.text = value;
        PlayerPrefs.SetInt("GameTime", int.Parse(value));
        PlayerPrefs.Save();
    }

    private void OnMemorizationTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 1)
        {
            value = "1";
        }
        else if (int.Parse(value) > 99)
        {
            value = "99";
        }
        _inputMemorizationTime.text = value;
        PlayerPrefs.SetInt("MemorizationTime", int.Parse(value));
        PlayerPrefs.Save();
    }

    private void OnRevealsToWinValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 1)
        {
            value = "1";
        }
        else if (int.Parse(value) > _memoryGame.Config.cardPairs.Length)
        {
            value = _memoryGame.Config.cardPairs.Length.ToString();
        }
        _inputRevealsToWin.text = value;
        PlayerPrefs.SetInt("RevealsToWin", int.Parse(value));
        PlayerPrefs.Save();
    }


    void UpdateInputValues()
    {
        int gameTime = PlayerPrefs.GetInt("GameTime");
        int memorizationTime = PlayerPrefs.GetInt("MemorizationTime");
        int revealsToWin = PlayerPrefs.GetInt("RevealsToWin");

        _inputGameTime.text = gameTime.ToString();
        _inputMemorizationTime.text = memorizationTime.ToString();
        _inputRevealsToWin.text = revealsToWin.ToString();
    }
}
