using TMPro;
using UnityEngine;

public class MemoryGameConfigMenu : ConfigMenu
{
    [Header("Game Config")]
    [SerializeField] private MemoryGame _memoryGame;

    [SerializeField] private TMP_InputField _inputGameTime;
    [SerializeField] private TMP_InputField _inputMemorizationTime;

    protected override void Awake()
    {
        base.Awake();
        SetInitialValues();
        _inputGameTime.onSubmit.AddListener(OnGameTimeValueChanged);
        _inputMemorizationTime.onSubmit.AddListener(OnMemorizationTimeValueChanged);
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
    }

    private void OnGameTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 0)
        {
            value = "0";
        }
        else if (int.Parse(value) > 120)
        {
            value = "120";
        }
        _inputGameTime.text = value;
        PlayerPrefs.SetInt("GameTime", int.Parse(value));
        PlayerPrefs.Save();
    }

    private void OnMemorizationTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 0)
        {
            value = "0";
        }
        else if (int.Parse(value) > 5)
        {
            value = "5";
        }
        _inputMemorizationTime.text = value;
        PlayerPrefs.SetInt("MemorizationTime", int.Parse(value));
        PlayerPrefs.Save();
    }

    void UpdateInputValues()
    {
        int gameTime = PlayerPrefs.GetInt("GameTime");
        int memorizationTime = PlayerPrefs.GetInt("MemorizationTime");

        _inputGameTime.text = gameTime.ToString();
        _inputMemorizationTime.text = memorizationTime.ToString();
    }
}
