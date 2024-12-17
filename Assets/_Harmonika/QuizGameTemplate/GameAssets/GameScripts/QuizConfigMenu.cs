using TMPro;
using UnityEngine;

public class NewMonoBehaviourScript : ConfigMenu
{
    [Header("Game Config")]
    [SerializeField] private QuizGame _quizGame;

    //[SerializeField] private TMP_InputField _inputGameTime;
    //[SerializeField] private TMP_InputField _inputMemorizationTime;

    protected override void Awake()
    {
        base.Awake();
        SetInitialValues();
        //_inputGameTime.onSubmit.AddListener(OnGameTimeValueChanged);
        //_inputMemorizationTime.onSubmit.AddListener(OnMemorizationTimeValueChanged);
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
            //PlayerPrefs.SetInt("GameTime", _quizGame.Config.gameTimer);
            PlayerPrefs.Save();
        }

        if (!PlayerPrefs.HasKey("MemorizationTime"))
        {
            //PlayerPrefs.SetInt("MemorizationTime", _quizGame.Config.memorizationTime);
            PlayerPrefs.Save();
        }
    }

    private void OnGameTimeValueChanged(string value)
    {

    }

    private void OnMemorizationTimeValueChanged(string value)
    {

    }

    void UpdateInputValues()
    {
        int gameTime = PlayerPrefs.GetInt("GameTime");
        int memorizationTime = PlayerPrefs.GetInt("MemorizationTime");

        //_inputGameTime.text = gameTime.ToString();
        //_inputMemorizationTime.text = memorizationTime.ToString();
    }
}
