using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Question
{
    [TextArea(1, 6)] public string question;
    [TextArea(1, 6)] public string rightAns;
    [TextArea(1, 6)] public string wrongAns1;
    [TextArea(1, 6)] public string wrongAns2;
    [TextArea(1, 6)] public string wrongAns3;
}

[CreateAssetMenu(fileName = "Quiz Config", menuName = "Harmonika/ScriptableObjects/Quiz Config", order = 1)]
public class QuizConfig : GameConfigScriptable
{
    [Space(5)]
    [Header("Configurable Variables")]
    public int rightAnswersToWin = 3;
    public int totalQuestionCount = 5;

    public bool randomizeAnswers = true;
    public bool randomizeQuestions = true;

    [Space(5)]
    [Header("Configurable Questions")]
    public GameObject answerPrefab;
    public List<Question> questions;
}