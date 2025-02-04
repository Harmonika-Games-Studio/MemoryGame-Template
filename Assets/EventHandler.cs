using Harmonika.Menu;
using System;
using UnityEngine;

public class EventHandler : MonoBehaviour
{
    [SerializeField] private MenuManager _menuManager;

    private void Awake()
    {
        RankingMenu.OnCloseRanking  += OnCloseRankingDo;
    }

    private void OnDestroy()
    {
        RankingMenu.OnCloseRanking -= OnCloseRankingDo;
    }

    private void OnCloseRankingDo()
    {
        _menuManager.OpenMenu("MainMenu");
        AppManager.Instance.OpenMenu("CloseAll");
    }
}
