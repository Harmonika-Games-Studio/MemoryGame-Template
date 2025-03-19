using TMPro;
using UnityEngine;

public class MemoryGameConfigMenu : ConfigMenu
{
    [Header("Game Config")]
    [SerializeField] private MemoryGame _memoryGame;

    [SerializeField] private TMP_InputField _inputGameTime;
    [SerializeField] private TMP_InputField _inputMemorizationTime;
    [SerializeField] private TMP_InputField _inputcorrectAnswers;

    protected override void Awake()
    {
        base.Awake();
        SetInitialValues();
        _inputGameTime.onSubmit.AddListener(OnGameTimeValueChanged);
        _inputMemorizationTime.onSubmit.AddListener(OnMemorizationTimeValueChanged);
        _inputcorrectAnswers.onSubmit.AddListener(OnCorrectAnswersValueChanged);
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

        if (!PlayerPrefs.HasKey("CorrectAnswers"))
        {
            PlayerPrefs.SetInt("CorrectAnswers", 3);
            PlayerPrefs.Save();
        }
    }

    private void OnGameTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 10)
        {
            value = "10";
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
        _inputMemorizationTime.text = value;
        PlayerPrefs.SetInt("MemorizationTime", int.Parse(value));
        PlayerPrefs.Save();
    }

    private void OnCorrectAnswersValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 1)
        {
            value = "1";
        }
        else if (int.Parse(value) > 6)
        {
            value = "6";
        }
        _inputcorrectAnswers.text = value;
        PlayerPrefs.SetInt("CorrectAnswers", int.Parse(value));
        PlayerPrefs.Save();
    }

    void UpdateInputValues()
    {
        int gameTime = PlayerPrefs.GetInt("GameTime");
        int memorizationTime = PlayerPrefs.GetInt("MemorizationTime");
        int correctAnswers = PlayerPrefs.GetInt("CorrectAnswers");

        _inputGameTime.text = gameTime.ToString();
        _inputMemorizationTime.text = memorizationTime.ToString();
        _inputcorrectAnswers.text = correctAnswers.ToString();
    }
}
