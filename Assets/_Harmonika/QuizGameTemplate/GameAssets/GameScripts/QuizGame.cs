using Harmonika.Menu;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Harmonika.Tools;

public class QuizGame : MonoBehaviour
{
    [Header("Quiz Configuration File")]
    [SerializeField] private QuizConfig _config;
    [SerializeField] private Toggle _toggle;

    [Space(5)]
    [Header("References")]
    [SerializeField] private Transform _answersGrid;
    [SerializeField] private TMP_Text _questionTxt;
    [SerializeField] private TMP_Text _questionNumbTxt;

    [Space(5)]
    [Header("Menus")]
    [SerializeField] private StartMenu _mainMenu;
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu;
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu2;
    [SerializeField] private VictoryMenu _victoryMenu;
    [SerializeField] private ParticipationMenu _participationMenu;
    [SerializeField] private LoseMenu _loseMenu;

    private MenuManager _gameMenu;
    private CanvasGroup _gameCV;
    private Question[] _questions;
    private int _actualQuestion;
    private int _rightAnswers = 0;
    string[] _answersTxt = new string[4];
    Answer[] _answers = new Answer[4];

    private float _startTime;
    private float _endTime;

    public QuizConfig Config { get => _config; }

    void Awake()
    {
        AppManager.Instance.gameConfig = _config;
        _gameCV = GetComponentInChildren<CanvasGroup>();
        _gameMenu = GetComponentInChildren<MenuManager>();
        SetupButtons();
        InstantiateQuestionPrefabs();
    }

    public void StartGame()
    {
        _startTime = Time.time;
        _rightAnswers = 0;
        _actualQuestion = 0;
        OrganizeQuestionsOrder();
        InitiateLevel(_actualQuestion);
    }

    public void ClickedOnAnswer(Answer answerClicked)
    {
        if (answerClicked.isCorrect) _rightAnswers++;

        foreach (var answer in _answers)
        {
            answer.button.interactable = false;

            if (answer != answerClicked)
            {
                if (answer.isCorrect) answer.ChangeColor(Color.green);
                else answer.ChangeColor(Color.red);
            }
        }
        if (_questions != null)
        { 
        if (_questions.Length > 0)
            StartCoroutine(NextQuestionRoutine(3));
        }
    }

    virtual protected void SetupButtons()
    {
        if (_config.useLeads)
        {
            _mainMenu.AddStartGameButtonListener(() => _gameMenu.OpenMenu("CollectLeadsMenu"));
            _mainMenu.AddStartGameButtonListener(() => _collectLeadsMenu.ClearAllFields());
            _collectLeadsMenu.AddContinueGameButtonListener(() => _gameMenu.OpenMenu("CollectLeadsMenu2"));
            _collectLeadsMenu.AddContinueGameButtonListener(() => _toggle.isOn = true);
            _collectLeadsMenu.AddBackButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
            _collectLeadsMenu2.AddContinueGameButtonListener(() => _gameMenu.CloseMenus());
            _collectLeadsMenu2.AddContinueGameButtonListener(StartGame);
            _collectLeadsMenu2.AddBackButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            _mainMenu.AddStartGameButtonListener(() => _gameMenu.CloseMenus());
            _mainMenu.AddStartGameButtonListener(StartGame);
        }

        _victoryMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _victoryMenu.AddBackToMainMenuButtonListener(StartGame);
        _loseMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _loseMenu.AddBackToMainMenuButtonListener(StartGame);
        _participationMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _participationMenu.AddBackToMainMenuButtonListener(StartGame);
    }

    void InstantiateQuestionPrefabs()
    {
        for (int i = 0; i < 4; i++)
        {
            _answers[i] = Instantiate(_config.answerPrefab, _answersGrid).GetComponent<Answer>();
            _answers[i].quizGame = this;
        }
    }

    void OrganizeQuestionsOrder()
    {
        int totalQuestionCount = _config.totalQuestionCount;
        _questions = new Question[totalQuestionCount];

        for (int i = 0; i < _config.questions.Count; i++)
        {
            List<Question> questList = _config.questions.Shuffle();
            if (_config.randomizeQuestions)
            {
                List<int> indexes = IntListInRandomOrder(0, totalQuestionCount);

                for (int j = 0; j < totalQuestionCount; j++)
                {
                    _questions[j] = _config.questions[indexes[j]];
                }
            }
            else
            {
                _questions[i] = _config.questions[i];
            }
        }
    }

    void InitiateLevel(int questionIndex)
    {
        List<int> indexes = IntListInRandomOrder(0, 4);

        _questionTxt.text = _questions[questionIndex].question;
        _questionNumbTxt.text = "Pergunta: " + (questionIndex + 1);
        _answersTxt[0] = _questions[questionIndex].rightAns;
        _answersTxt[1] = _questions[questionIndex].wrongAns1;
        _answersTxt[2] = _questions[questionIndex].wrongAns2;
        _answersTxt[3] = _questions[questionIndex].wrongAns3;

        for (int i = 0; i < 4; i++)
        {
            char letter = (char)('A' + i);
            _answers[i].ansText.text = $"{letter}) {_answersTxt[indexes[i]]}";

            if (_answersTxt[indexes[i]] == _questions[questionIndex].rightAns)
                _answers[i].isCorrect = true;
            else
                _answers[i].isCorrect = false;
        }
    }

    void EndGame()
    {
        AppManager.Instance.DataSync.AddDataToJObject("tempo", Time.time - _startTime);
        if (_rightAnswers >= _config.rightAnswersToWin)
        {
            Win();
        }
        else 
        {
            Lose();
        }
    }

    void Win()
    {
        string prize = AppManager.Instance.Storage.GetRandomPrize();

        if (!string.IsNullOrEmpty(prize))
        {
            _gameMenu.OpenMenu("VictoryMenu");
            _victoryMenu.ChangePrizeText(prize);
        }
        else
        {
            _gameMenu.OpenMenu("ParticipationMenu");
        }
        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "sim");
        //AppManager.Instance.DataSync.AddDataToJObject("premio", prize);
        AppManager.Instance.DataSync.AddDataToJObject("pontos", _rightAnswers.ToString());
        AppManager.Instance.DataSync.SendLeads();
    }

    void Lose()
    {
        _gameMenu.OpenMenu("LoseMenu");
        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "não");
        //AppManager.Instance.DataSync.AddDataToJObject("premio", "nenhum");
        AppManager.Instance.DataSync.AddDataToJObject("pontos", _rightAnswers.ToString());
        AppManager.Instance.DataSync.SendLeads();
    }

    List<int> IntListInRandomOrder(int start, int end)
    {
        List<int> indexes = Enumerable.Range(start, end).ToList();

        for (int j = 0; j < indexes.Count; j++)
        {
            int temp = indexes[j];
            int randomIndex = Random.Range(j, indexes.Count);
            indexes[j] = indexes[randomIndex];
            indexes[randomIndex] = temp;
        }
        return indexes;
    }

    private IEnumerator NextQuestionRoutine(int delay, float transition = .6f)
    {
        _actualQuestion++;
        yield return new WaitForSeconds(delay);

        #region FadeOut
        float time = 0;
        _gameCV.interactable = false;
        _gameCV.blocksRaycasts = false;
        while (time < (transition / 2))
        {
            time += Time.deltaTime;
            _gameCV.alpha = Mathf.Lerp(0, 1, time / (transition / 2));
            yield return null;
        }
        _gameCV.alpha = 1;
        #endregion

        foreach (var answer in _answers)
        {
            answer.ChangeColor(Color.white);
            answer.button.interactable = true;
            answer.isCorrect = false;
        }

        if (_actualQuestion < _config.totalQuestionCount)
        {
            InitiateLevel(_actualQuestion);
        }
        else
        {
            EndGame();
        }

        #region FadeIn
        time = 0;
        _gameCV.interactable = false;
        _gameCV.blocksRaycasts = false;
        while (time < (transition / 2))
        {
            time += Time.deltaTime;
            _gameCV.alpha = Mathf.Lerp(1, 0, time / (transition / 2));
            yield return null;
        }
        _gameCV.alpha = 0;
        #endregion
    }
}