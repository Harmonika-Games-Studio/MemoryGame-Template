using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq.Expressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Turn
{
    on,
    off
}

[System.Serializable] public class Menu 
{
    public string name;
    public CanvasGroup group;
}
public class GameMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup menuBackground;
    [SerializeField] private Menu[] menus;
    [SerializeField] private SicrediFormController form;
    private JObject savedData = new JObject();

    public static Action OnEndGame;
    public static Action OnStartGame;

    private void Awake()
    {
        OnStartGame += VerifyStock;
        OnEndGame += RestartGame;
    }

    public void Start()
    {
        OpenMenu(menus[0].name);

        EstoqueHandler.instance.OnSetupStorage.Invoke();
    }

    public void Update() {

#if UNITY_EDITOR
        //if (Input.GetKeyDown(KeyCode.Alpha0)) {
        //    Debug.Log(EstoqueHandler.instance.GetRandomPrize());
        //}
#endif
    }

    public void OnDisable()
    {
        OnStartGame -= VerifyStock;
        OnEndGame -= RestartGame;
    }

    #region coisas que não deveriam estar aqui, mas a gente n tem tempo
    public void RestartGame()
    {
        OpenMenu(menus[0].name);
        SceneManager.LoadScene(0);
    }

    public void VerifyStock()    
    { 
        //ITS NOT VERIFYING STORAGE
        OpenLeadCollect();
    }

    public void OpenLeadCollect()
    {
        OpenMenu(menus[1].name);
    }

    #endregion

    public void OpenMenu(string menuName)
    {
        Menu m = Array.Find(menus, menu => menu.name == menuName);
        if (m == null)
        {
            Debug.LogError("There is no one Menu with the name " + menuName + "!\n" +
                "Please, use a valid menu name");
            return;
        }

        TurnBackground(Turn.on);
        foreach (Menu menu in menus)
        {
            if (menu.name == menuName)
            {
                TurnMenu(menu.group, Turn.on);
            }
            else 
            {
                TurnMenu(menu.group, Turn.off);
            }
        }
    }

    public void CloseMenu()
    {
        TurnBackground(Turn.off);
        foreach (Menu menu in menus)
        {
                TurnMenu(menu.group, Turn.off);
        } 
    }

    void TurnMenu(CanvasGroup group, Turn turn)
    {
        switch (turn)
        {
            case Turn.on:
                group.alpha = 1;
                group.interactable = true;
                group.blocksRaycasts = true;
                break;
            case Turn.off:
                group.alpha = 0;
                group.interactable = false;
                group.blocksRaycasts = false;
                break;
        }
    }

    void TurnBackground(Turn turn)
    {
        switch (turn)
        {
            case Turn.on:
                menuBackground.alpha = 1;
                menuBackground.blocksRaycasts = true;
                break;
            case Turn.off:
                menuBackground.alpha = 0;
                menuBackground.blocksRaycasts = false;
                break;
        }
    }
}
