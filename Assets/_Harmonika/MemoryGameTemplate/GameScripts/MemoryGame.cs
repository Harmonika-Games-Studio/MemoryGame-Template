using Harmonika.Tools;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JsonDeserializedConfig
{
    public string gameName;
    public string cardBack;
    public string[] cardsList;
    public bool useLeads;
    public StorageItemConfig[] storageItems;

    // Construtor padrão necessário para a desserialização
    [JsonConstructor]
    public JsonDeserializedConfig(
        string gameName = "",
        string cardBack = "",
        string[] cardsList = null,
        bool useLeads = false,
        StorageItemConfig[] storageItems = null)
    {
        this.gameName = gameName;
        this.cardBack = cardBack;
        this.cardsList = cardsList ?? Array.Empty<string>();
        this.useLeads = useLeads;
        this.storageItems = storageItems ?? Array.Empty<StorageItemConfig>();
    }
}

public class MemoryGame : MonoBehaviour
{
    [SerializeField] private MemoryGameConfig _config;
    
    [Header("References")]
    [SerializeField] private Cronometer _cronometer;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Menus")]
    [SerializeField] private StartMenu _mainMenu;
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu;
    [SerializeField] private GameoverMenu _victoryMenu;
    [SerializeField] private GameoverMenu _participationMenu;
    [SerializeField] private GameoverMenu _loseMenu;
    [SerializeField] private Button _option1;
    [SerializeField] private Button _option2;
    [SerializeField] private Button _option3;
    [SerializeField] private Button _option4;

    private int _revealedPairs;
    private int _remainingTime;
    private bool _canClick = true;
    private float _startTime;
    private MenuManager _gameMenu;
    private RectTransform _gridLayoutRect;

    private MemoryGameCard _lastClickedCard;
    private List<MemoryGameCard> _cardsList = new List<MemoryGameCard>();

    #region Properties

    public MemoryGameConfig Config { get => _config; }

    public bool CanClick
    {
        get
        {
            return _canClick;
        }
    }
    #endregion

    private void Awake()
    {
        //This code is necessary to run the game smoothly on android
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        _gridLayoutRect = gridLayoutGroup.gameObject.GetComponent<RectTransform>();
        _cronometer.onEndTimer.AddListener(() => EndGame(false));
        AppManager.Instance.gameConfig = _config;

        _gameMenu = GetComponentInChildren<MenuManager>();

        AppManager.Instance.ApplyScriptableConfig();
        AppManager.Instance.Storage.Setup();
        
        SetupButtons();
    }

    public void StartGame()
    {
        if (AppManager.Instance.Storage.InventoryCount <= 0)
        {
            PopupManager.Instance.InvokeConfirmDialog("Nenhum item no estoque\n" +
                "Insira algum prêmio para continuar com a ativação", "OK", true);

            return;
        }
        _gameMenu.CloseMenus();

        _startTime = Time.time;
        _cronometer.totalTimeInSeconds = PlayerPrefs.GetInt("GameTime", _config.gameTime);
        _remainingTime = _cronometer.totalTimeInSeconds;
        int minutes = _remainingTime / 60;
        int seconds = _remainingTime % 60;
        if (_cronometer.useFormat) _cronometer.TimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        else _cronometer.TimerText.text = _remainingTime.ToString();

        InstantiateCards();
        AdjustGridLayout();
        ShuffleCards();

        InvokeUtility.Invoke(PlayerPrefs.GetInt("MemorizationTime", _config.memorizationTime), () =>
        {
            _cronometer.StartTimer();

            foreach (var card in _cardsList)
            {
                card.RotateCardDown();
            }
        });
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

        // Se você estiver usando o GridLayoutGroup, force a atualização do layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(_gridLayoutRect);
    }

    public void ClickedOnCard(MemoryGameCard card)
    {
        SoundSystem.Instance.Play("Card", true);

        _canClick = false;
        Invoke(nameof(ReenableClick), .2f);

        if (_lastClickedCard == null)
            _lastClickedCard = card;
        else
        {
            if (_lastClickedCard.id == card.id)
            {
                _lastClickedCard.IsCorect = true;
                card.IsCorect = true;
                _revealedPairs++;
                if (_revealedPairs >= 3)
                {
                    EndGame(true, AppManager.Instance.Storage.GetRandomPrize());
                }
            }
            else
            {
                _lastClickedCard.IsCorect = false;
                card.IsCorect = false;
            }

            _lastClickedCard = null;
        }
    }

    private void ReenableClick()
    {
        _canClick = true;
    }

    private void SetupButtons()
    {
        if (AppManager.Instance.gameConfig.useLeads)
        {
            _mainMenu.StartBtn.onClick.AddListener(() =>
            {
                if (AppManager.Instance.Storage.InventoryCount <= 0)
                {
                    PopupManager.Instance.InvokeConfirmDialog("Nenhum item no estoque\n" +
                        "Insira algum prêmio para continuar com a ativação", "OK", true);

                    return;
                }

                _gameMenu.OpenMenu("CollectLeadsMenu");
                _collectLeadsMenu.ClearAllFields();
            });
            _collectLeadsMenu.ContinueBtn.onClick.AddListener(() => _gameMenu.OpenMenu("QuestionMenu"));
            _collectLeadsMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            _mainMenu.StartBtn.onClick.AddListener(StartGame);
        }

        _option1.onClick.AddListener(() =>
        {
            StartGame();
            AppManager.Instance.DataSync.AddDataToJObject("custom1", "Detox Corporativo");
        });
        _option2.onClick.AddListener(() =>
        {
            StartGame();
            AppManager.Instance.DataSync.AddDataToJObject("custom1", "Equilíbrio Zen");
        });
        _option3.onClick.AddListener(() =>
        {
            StartGame();
            AppManager.Instance.DataSync.AddDataToJObject("custom1", "Ritmo de Performance");
        });
        _option4.onClick.AddListener(() =>
        {
            StartGame();
            AppManager.Instance.DataSync.AddDataToJObject("custom1", "Up Total");
        });
        _victoryMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        _loseMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        _participationMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
    }

    private void InstantiateCards()
    {
        Debug.Log("Instantiate");

        _config.cardPairs.Shuffle();

        for (int i = 0; i < _config.cardPairs.Length; i++)
        //for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                GameObject newCard = Instantiate(_config._cardPrefab, gridLayoutGroup.transform);
                MemoryGameCard cardConfig = newCard.GetComponent<MemoryGameCard>();
                cardConfig.id = i;
                cardConfig.manager = this;
                cardConfig.cardBack = _config.cardBack;
                cardConfig.cardFront = _config.cardPairs[i];

                _cardsList.Add(cardConfig);
            }
        }
    }

    private void AdjustGridLayout()
    {
        float originalCellWidth = gridLayoutGroup.cellSize.x;
        float originalCellHeight = gridLayoutGroup.cellSize.y;

        int totalCards = _config.cardPairs.Length * 2;

        int numberOfColumns = 4;
        int numberOfRows = 5;
        // Priorizar o maior número de colunas possível
        /*int numberOfColumns = totalCards; // Começamos assumindo todas as cartas em uma linha
        int numberOfRows = 1;

        // Procurar uma combinação onde o número de colunas é maior ou igual ao de linhas
        for (int i = Mathf.CeilToInt(Mathf.Sqrt(totalCards)); i <= totalCards; i++)
        {
            if (totalCards % i == 0) // Se não sobrar resto, encontramos uma divisão exata
            {
                numberOfColumns = i;
                numberOfRows = totalCards / i;
                if (numberOfColumns >= numberOfRows) // Priorizamos mais colunas que linhas
                    break;
            }
        }*/

        float gridWidth = _gridLayoutRect.rect.width;
        float gridHeight = _gridLayoutRect.rect.height;

        // Obter o padding do GridLayoutGroup
        float totalHorizontalPadding = gridLayoutGroup.padding.left + gridLayoutGroup.padding.right;
        float totalVerticalPadding = gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom;

        // Calcular o tamanho máximo das células, considerando o padding
        //float maxCellWidth = (gridWidth - totalHorizontalPadding) / numberOfColumns;
        //float maxCellHeight = (gridHeight - totalVerticalPadding) / numberOfRows;

        float maxCellWidth = 175;
        float maxCellHeight = 271;

        // Calcular o fator de escala para manter a proporção
        float widthScale = maxCellWidth / originalCellWidth;
        float heightScale = maxCellHeight / originalCellHeight;

        // Usar o menor fator de escala para garantir que as células se ajustem
        float scaleFactor = Mathf.Min(widthScale, heightScale);

        // Aplicar o fator de escala ao tamanho original das células
        float cellWidth = originalCellWidth * scaleFactor;
        float cellHeight = originalCellHeight * scaleFactor;

        // Configurar o GridLayoutGroup com a quantidade de colunas
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = numberOfColumns;
        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);

    }

    private void EndGame(bool win, string prizeName = null)
    {
        int inventoryCount = AppManager.Instance.Storage.InventoryCount;

        if (inventoryCount <= 0)
            PopupManager.Instance.InvokeToast("O estoque está vazio!", 3, ToastPosition.LowerMiddle);
        else if (inventoryCount == 1)
            PopupManager.Instance.InvokeToast($"{inventoryCount} prêmio restante no estoque!", 3, ToastPosition.LowerMiddle);
        else if (inventoryCount <= 3)
            PopupManager.Instance.InvokeToast($"{inventoryCount} prêmios restantes no estoque!", 3, ToastPosition.LowerMiddle);

        _cronometer.EndTimer();
        float tempo = Time.time - _startTime;
        AppManager.Instance.DataSync.AddDataToJObject("tempo", tempo);
        AppManager.Instance.DataSync.AddDataToJObject("pontos", (int)Math.Floor(_config.gameTime - tempo));

        InvokeUtility.Invoke(1f, () =>
        {
            if (win) WinGame(prizeName);
            else LoseGame();

            foreach (var card in _cardsList)
            {
                Destroy(card.gameObject);
            }
        _cardsList.Clear();
        _revealedPairs = 0;

        AppManager.Instance.DataSync.SaveLeads();
        });
    }

    private void WinGame(string prizeName = null)
    {
        SoundSystem.Instance.Play("Win");

        if (!string.IsNullOrEmpty(prizeName))
        {
            //_victoryMenu.SecondaryText = $"Você ganhou um <b>{prizeName}</b>!";
            prizeName = "Nenhum";
            _victoryMenu.SecondaryText = $"Você conseguiu!";
            _gameMenu.OpenMenu("VictoryMenu");
        }
        else
        {
            prizeName = "Nenhum";
            _victoryMenu.SecondaryText = $"Você conseguiu!";
            _gameMenu.OpenMenu("VictoryMenu");
        }

        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "sim");
        AppManager.Instance.DataSync.AddDataToJObject("brinde", prizeName);
    }

    private void LoseGame()
    {
        SoundSystem.Instance.Play("Fail");

        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "não");
        AppManager.Instance.DataSync.AddDataToJObject("brinde", "nenhum");

        _gameMenu.OpenMenu("LoseMenu");
    }
}