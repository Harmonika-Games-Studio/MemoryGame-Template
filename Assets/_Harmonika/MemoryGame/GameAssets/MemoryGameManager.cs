using Harmonika.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _cardsGrid;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private RectTransform gridRectTransform;
    [SerializeField] private MenuManager _gameMenu;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Image _timerBackground;
    [SerializeField] private MemoryGameConfig _memoryGameConfig;

    [Header("Menus")]
    [SerializeField] private MainMenu _mainMenu;
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu;
    [SerializeField] private VictoryMenu _victoryMenu;
    [SerializeField] private ParticipationMenu _participationMenu;
    [SerializeField] private LoseMenu _loseMenu;
    
    public int totalTimeInSeconds = 0;
    public int memorizationTime = 0;

    private int _revealedPairs = 0;
    private int _remainingTime = 0;
    public bool _canClick = true;
    private Sprite _cardBack;
    private Sprite[] _cardPairs;
    private Coroutine _gameTimer;

    private MemoryGameCard lastClickedCard;

    private List<MemoryGameCard> _cardsList = new List<MemoryGameCard>();

    private void Start()
    {
        AppManager.Instance.gameConfig = _memoryGameConfig;

        SetupGameConfigFromScriptable(_memoryGameConfig);
        SetupButtons();
    }

    public IEnumerator StartGame()
    {
        InstantiateCards();
        AdjustGridLayout();
        ShuffleCards();
        yield return new WaitForSeconds(memorizationTime);

        foreach (var card in _cardsList)
        {
            card.RotateCardDown();
        }

        _gameTimer = StartCoroutine(GameTimer());
    }

    public void ShuffleCards()
    {
        // Embaralha a lista de cartas
        for (int i = 0; i < _cardsList.Count; i++)
        {
            MemoryGameCard temp = _cardsList[i];
            int randomIndex = UnityEngine.Random.Range(i, _cardsList.Count);
            _cardsList[i] = _cardsList[randomIndex];
            _cardsList[randomIndex] = temp;
        }

        // Reposiciona as cartas na grid
        for (int i = 0; i < _cardsList.Count; i++)
        {
            _cardsList[i].transform.SetSiblingIndex(i); // Define a nova ordem na grid
        }

        // Se voc� estiver usando o GridLayoutGroup, force a atualiza��o do layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRectTransform);
    }

    public void ClickedOnCard(MemoryGameCard card)
    {
        SoundSystem.Instance.Play("Card", true);

        _canClick = false;
        StartCoroutine(InvokeTimerCoroutine(.2f, ReenableClick));

        if (lastClickedCard == null)
            lastClickedCard = card;
        else
        {
            if (lastClickedCard.id == card.id)
            {
                lastClickedCard.IsCorect = true;
                card.IsCorect = true;
                _revealedPairs++;
                if (_revealedPairs >= _cardPairs.Length)
                {
                    StopCoroutine(_gameTimer);
                    StartCoroutine(InvokeTimerCoroutine(1, () => WinGame(AppManager.Instance.Storage.GetRandomPrize())));
                }
            }
            else
            {
                lastClickedCard.IsCorect = false;
                card.IsCorect = false;
            }

            lastClickedCard = null;
        }
    }

    private void ReenableClick()
    {
        _canClick = true;
    }

    public void SetupGameConfigFromScriptable(MemoryGameConfig config)
    {
        if (config != null)
        {
            //_mainMenu.ChangeVisualIdentity(config.primaryClr);
            //_victoryMenu.ChangeVisualIdentity(config.primaryClr);
            //_participationMenu.ChangeVisualIdentity(config.primaryClr);
            //_loseMenu.ChangeVisualIdentity(config.primaryClr); 
            memorizationTime = config.memorizationTime;
            totalTimeInSeconds = config.gameTimer;
            _mainMenu.TitleText = config.gameName;
            //_victoryMenu.VictoryText = config.victoryMainText;
            //_loseMenu.LoseText = config.loseMainText;
            _cardBack = config.cardBack;
            _cardPairs = config.cardPairs;
            AppManager.Instance.gameConfig.useLeads = config.useLeads;
            AppManager.Instance.Storage.itemsConfig = config.storageItems;
            //_timerBackground.color = _config.primaryClr;
            //_timerText.color = _config.primaryClr;

            if (AppManager.Instance.gameConfig.useLeads)
            {
                //_collectLeadsMenu.LeadsText = config.leadsText;
                //_collectLeadsMenu.ChangeVisualIdentity(config.leadsTextClr);
            }

            AppManager.Instance.Storage.Setup();
        }
    }

    private void SetupButtons()
    {
        if (AppManager.Instance.gameConfig.useLeads)
        {
            _mainMenu.AddStartGameButtonListener(() => _gameMenu.OpenMenu("CollectLeadsMenu"));
            _mainMenu.AddStartGameButtonListener(() => _collectLeadsMenu.ClearTextFields());
            _collectLeadsMenu.AddContinueGameButtonListener(() => _gameMenu.CloseMenus());
            _collectLeadsMenu.AddContinueGameButtonListener(() => StartCoroutine(StartGame()));
            _collectLeadsMenu.AddBackButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            _mainMenu.AddStartGameButtonListener(() => _gameMenu.CloseMenus());
            _mainMenu.AddStartGameButtonListener(() => StartCoroutine(StartGame()));
        }

        _victoryMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _loseMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _participationMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
    }

    private void InstantiateCards()
    {
        Debug.Log("Instantiate");

        for (int i = 0; i < _cardPairs.Length; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                GameObject newCard = Instantiate(_cardPrefab, _cardsGrid);
                MemoryGameCard cardConfig = newCard.GetComponent<MemoryGameCard>();
                cardConfig.id = i;
                cardConfig.manager = this;
                cardConfig.cardBack = _cardBack;
                cardConfig.cardFront = _cardPairs[i];

                _cardsList.Add(cardConfig);
            }
        }
    }

    private void AdjustGridLayout()
    {
        float originalCellWidth = gridLayoutGroup.cellSize.x;
        float originalCellHeight = gridLayoutGroup.cellSize.y;

        int totalCards = _cardPairs.Length * 2;

        // Priorizar o maior n�mero de colunas poss�vel
        int numberOfColumns = totalCards; // Come�amos assumindo todas as cartas em uma linha
        int numberOfRows = 1;

        // Procurar uma combina��o onde o n�mero de colunas � maior ou igual ao de linhas
        for (int i = Mathf.CeilToInt(Mathf.Sqrt(totalCards)); i <= totalCards; i++)
        {
            if (totalCards % i == 0) // Se n�o sobrar resto, encontramos uma divis�o exata
            {
                numberOfColumns = i;
                numberOfRows = totalCards / i;
                if (numberOfColumns >= numberOfRows) // Priorizamos mais colunas que linhas
                    break;
            }
        }

        float gridWidth = gridRectTransform.rect.width;
        float gridHeight = gridRectTransform.rect.height;

        // Obter o padding do GridLayoutGroup
        float totalHorizontalPadding = gridLayoutGroup.padding.left + gridLayoutGroup.padding.right;
        float totalVerticalPadding = gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom;

        // Calcular o tamanho m�ximo das c�lulas, considerando o padding
        float maxCellWidth = (gridWidth - totalHorizontalPadding) / numberOfColumns;
        float maxCellHeight = (gridHeight - totalVerticalPadding) / numberOfRows;

        // Calcular o fator de escala para manter a propor��o
        float widthScale = maxCellWidth / originalCellWidth;
        float heightScale = maxCellHeight / originalCellHeight;

        // Usar o menor fator de escala para garantir que as c�lulas se ajustem
        float scaleFactor = Mathf.Min(widthScale, heightScale);

        // Aplicar o fator de escala ao tamanho original das c�lulas
        float cellWidth = originalCellWidth * scaleFactor;
        float cellHeight = originalCellHeight * scaleFactor;

        // Configurar o GridLayoutGroup com a quantidade de colunas
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = numberOfColumns;
        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);

    }

    private void WinGame(string prizeName = null)
    {
        SoundSystem.Instance.Play("Win");

        if (!string.IsNullOrEmpty(prizeName))
        {
            _victoryMenu.ChangePrizeText("(" + prizeName + ")");
            _gameMenu.OpenMenu("VictoryMenu");
        }
        else
        {
            prizeName = "Nenhum";
            _gameMenu.OpenMenu("ParticipationMenu");
        }

        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "sim");
        AppManager.Instance.DataSync.AddDataToJObject("premio", prizeName);
        AppManager.Instance.DataSync.AddDataToJObject("pontos", _remainingTime.ToString());

        EndGame();
    }

    private void LoseGame()
    {
        SoundSystem.Instance.Play("Fail");

        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "n�o");
        AppManager.Instance.DataSync.AddDataToJObject("premio", "nenhum");
        AppManager.Instance.DataSync.AddDataToJObject("pontos", _remainingTime.ToString());

        _gameMenu.OpenMenu("LoseMenu");
        
        EndGame();
    }

    private void EndGame()
    {
        foreach (var card in _cardsList)
        {
            Destroy(card.gameObject);
        }
        _cardsList.Clear();
        _revealedPairs = 0;
        _timerText.text = "00:00";

        AppManager.Instance.DataSync.SendLeads();
    }

    IEnumerator InvokeTimerCoroutine(float time, Action action)
    {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }

    IEnumerator GameTimer()
    {
        _remainingTime = totalTimeInSeconds;
        while (_remainingTime > 0)
        {
            int minutes = _remainingTime / 60;
            int seconds = _remainingTime % 60;

            _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            yield return new WaitForSeconds(1);

            _remainingTime--;
        }

        LoseGame();
    }
}