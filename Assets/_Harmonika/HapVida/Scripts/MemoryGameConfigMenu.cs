using TMPro;
using UnityEngine;

public class MemoryGameConfigMenu : ConfigMenu
{
    [Space(5)]
    [Header("Game Config ")]
    [SerializeField] private TMP_InputField _inputMemTimer;
    [SerializeField] private TMP_InputField _inputGameDuration;

    [Space(5)]
    [Header("Game Manager Reference ")]
    [SerializeField] private MemoryGameManager _memoryGameManager;

    private void Awake()
    {
        _inputMemTimer.onSubmit.AddListener(OnMemorizationTimeValueChanged);
        _inputGameDuration.onSubmit.AddListener(OnGameDurationValueChanged);
    }

    void Start()
    {
        UpdateInputValues();
    }

    void OnMemorizationTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 0)
        {
            value = "0";
        }

        _memoryGameManager.MemorizationTime = int.Parse(value);
    }

    void OnGameDurationValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || int.Parse(value) <= 0)
        {
            value = "0";
        }

        _memoryGameManager.GameDuration = int.Parse(value);
    }

    protected override void UpdateInputValues()
    {
        base.UpdateInputValues();

        _inputMemTimer.text = _memoryGameManager.MemorizationTime.ToString();
        _inputGameDuration.text = _memoryGameManager.GameDuration.ToString();
    }
}
