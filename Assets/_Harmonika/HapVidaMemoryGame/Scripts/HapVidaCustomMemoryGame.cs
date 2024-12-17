using Harmonika.Tools;
using UnityEngine;
using UnityEngine.UI;

public class HapVidaCustomMemoryGame : MemoryGame
{
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu2;
    [SerializeField] private Toggle _toggle;

    override protected void SetupButtons()
    {
        if (AppManager.Instance.gameConfig.useLeads)
        {
            base._mainMenu.AddStartGameButtonListener(() => base._gameMenu.OpenMenu("CollectLeadsMenu"));
            base._mainMenu.AddStartGameButtonListener(() => base._collectLeadsMenu.ClearAllFields());
            base._collectLeadsMenu.AddContinueGameButtonListener(() => base._gameMenu.OpenMenu("CollectLeadsMenu2"));
            //base._collectLeadsMenu.AddContinueGameButtonListener(() => _collectLeadsMenu2.ClearAllFields());
            base._collectLeadsMenu.AddContinueGameButtonListener(() => _toggle.isOn = true);
            _collectLeadsMenu2.AddContinueGameButtonListener(() => base._gameMenu.CloseMenus());
            _collectLeadsMenu2.AddContinueGameButtonListener(() => StartCoroutine(StartGame()));
            base._collectLeadsMenu.AddBackButtonListener(() => base._gameMenu.OpenMenu("MainMenu"));
            _collectLeadsMenu2.AddBackButtonListener(() => base._gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            base._mainMenu.AddStartGameButtonListener(() => base._gameMenu.CloseMenus());
            base._mainMenu.AddStartGameButtonListener(() => StartCoroutine(StartGame()));
        }

        base._victoryMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        base._loseMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        base._participationMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
    }
}