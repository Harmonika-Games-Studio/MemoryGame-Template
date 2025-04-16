using DG.Tweening;
using Harmonika.Tools;
using NaughtyAttributes;
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
    [Expandable][SerializeField] private MemoryGameConfig _config;
    
    [Header("References")]
    [SerializeField] private Cronometer _cronometer;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Menus")]
    [SerializeField] private StartMenu _mainMenu;
    [SerializeField] private CollectLeadsMenu _collectLeadsMenu;
    [SerializeField] private GameoverMenu _victoryMenu;
    [SerializeField] private GameoverMenu _participationMenu;
    [SerializeField] private GameoverMenu _loseMenu;
    [SerializeField] private Transform _startPosition;
    [SerializeField] private GameObject _transition;
    //[SerializeField] private GameObject _Timer;

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
        ShuffleArray(_config.cardPairs);
        InstantiateCards();
        AdjustGridLayout();
        ShuffleCards();
        _transition.SetActive(false);
        _transition.SetActive(true);
        AnimateCards(0.2f, FinishCardSetup);

        //InvokeUtility.Invoke(PlayerPrefs.GetFloat("MemorizationTime", _config.memorizationTime), () =>
        //{
        //    _cronometer.StartTimer();
        //
        //    foreach (var card in _cardsList)
        //    {
        //        card.RotateCardDown();
        //    }
        //});
    }

    private void ShuffleArray(Sprite[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, array.Length);
            (array[i], array[randomIndex]) = (array[randomIndex], array[i]); // Troca os valores
        }
    }

    private void AnimateCards(float movementDelay = 0.1f, Action onAllCardsPlaced = null)
    {
        Debug.Log("Animate Cards");
        int completedCards = 0;
        int totalCards = _cardsList.Count;

        for (int i = 0; i < _cardsList.Count; i++)
        {
            RectTransform cardRectTransform = _cardsList[i].transform.GetChild(0).GetComponent<RectTransform>();

            // Set initial world position
            cardRectTransform.position = _startPosition.position;

            // Move card to local zero position
            cardRectTransform.DOLocalMove(Vector3.zero, 0.35f)
                .SetDelay(i * movementDelay)
                .OnComplete(() =>
                {
                    completedCards++;

                    if (completedCards >= totalCards)
                    {
                        Debug.Log("All cards have been placed");
                        onAllCardsPlaced?.Invoke();
                    }
                });
        }
    }

    private void FinishCardSetup()
    {
        InvokeUtility.Invoke(PlayerPrefs.GetFloat("MemorizationTime", _config.memorizationTime), () =>
        {
            //_Timer.SetActive(true);
            _cronometer.StartTimer();
            //_bgTimer.SetActive(true);

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
                if (_revealedPairs >= 10)
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
            _collectLeadsMenu.ContinueBtn.onClick.AddListener(StartGame);
            _collectLeadsMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            _mainMenu.StartBtn.onClick.AddListener(StartGame);
        }

        _victoryMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        _loseMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
        _participationMenu.BackBtn.onClick.AddListener(() => _gameMenu.OpenMenu("MainMenu"));
    }

    private void InstantiateCards()
    {
        Debug.Log("Instantiate");

        for (int i = 0; i < 10; i++)
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

        int totalCards = 20;

        // Priorizar o maior número de colunas possível
        int numberOfColumns = 4; // Começamos assumindo todas as cartas em uma linha
        int numberOfRows = 5;

        // Procurar uma combinação onde o número de colunas é maior ou igual ao de linhas
        /*for (int i = Mathf.CeilToInt(Mathf.Sqrt(totalCards)); i <= totalCards; i++)
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
        float maxCellWidth = (gridWidth - totalHorizontalPadding) / numberOfColumns;
        float maxCellHeight = (gridHeight - totalVerticalPadding) / numberOfRows;

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
        //gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);

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
            _victoryMenu.SecondaryText = $"Você conseguiu!";
            _gameMenu.OpenMenu("VictoryMenu");
        }
        else
        {
            prizeName = "Nenhum";
            _gameMenu.OpenMenu("ParticipationMenu");
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