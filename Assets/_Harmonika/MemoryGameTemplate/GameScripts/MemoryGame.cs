using Harmonika.Tools;
using NaughtyAttributes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;


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
    [SerializeField] private Button _tryAgainButton;

    [Header("Rest")]
    [SerializeField] private Button _openRestButton;
    [SerializeField] private VideoPlayer _restVideo;
    [SerializeField] private Button _closeRestButton;
    [SerializeField] private Button _closeRestButtonAutomatic;
    
    private int _revealedPairs;
    private int _remainingTime;
    private bool _canClick = true;
    private float _startTime;
    private MenuManager _gameMenu;
    private RectTransform _gridLayoutRect;
    private float _idleTimer = 0f;
    private bool _timerActive = false;
    private const float IDLE_TIMEOUT = 30f; // 30 seconds before triggering idle action

    private bool _secondTry = true;
    [SerializeField] private bool _inMainMenu = true;
    private int _restClicks;

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
        //Application.targetFrameRate = 60;
        //QualitySettings.vSyncCount = 0;
        _gridLayoutRect = gridLayoutGroup.gameObject.GetComponent<RectTransform>();
        _cronometer.onEndTimer.AddListener(() => EndGame(false));
        AppManager.Instance.gameConfig = _config;
        _gameMenu = GetComponentInChildren<MenuManager>();
        AppManager.Instance.ApplyScriptableConfig();
        AppManager.Instance.Storage.Setup();

        // Load video from StreamingAssets
        LoadVideoFromStreamingAssets();

        _restVideo.Prepare();

        SetupButtons();
    }

    private void LoadVideoFromStreamingAssets()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "VideoVertical.mp4");

        // Set the video URL/path
        _restVideo.url = videoPath;
    }

    void FixedUpdate()
    {
        // Only track idle time if we're on the main menu
        if (_inMainMenu)
        {
            // Check for any user input including touch for mobile devices using the new Input System
            bool userInput = false;

            // Check for keyboard, mouse, or gamepad activity
            if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
                userInput = true;
            else if (Mouse.current != null && (Mouse.current.delta.ReadValue().magnitude > 0 || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed))
                userInput = true;
            else if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
                userInput = true;
            // Check for touch input for mobile devices
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                userInput = true;

            if (userInput)
            {
                // Reset timer when user interacts
                _idleTimer = 0f;
                _timerActive = true;
            }
            else if (_timerActive)
            {
                // Increment timer when no input is detected
                _idleTimer += Time.deltaTime;

                // Check if idle timeout has been reached
                if (_idleTimer >= IDLE_TIMEOUT)
                {
                    // Open the rest screen automatically
                    OpenRestScreen(true);

                    // Stop the timer until user returns to main menu
                    _timerActive = false;
                    _idleTimer = 0f;
                }
            }
        }
        else
        {
            // Reset timer when not on main menu
            _idleTimer = 0f;
            _timerActive = false;
        }
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
                if (_revealedPairs >= _config.cardPairs.Length)
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
            _collectLeadsMenu.ContinueBtn.onClick.AddListener(() => {
                _inMainMenu = false;
                _secondTry = false;
                StartGame();
                _tryAgainButton.gameObject.SetActive(true);
            });
            _collectLeadsMenu.BackBtn.onClick.AddListener(() => { 
                _gameMenu.OpenMenu("MainMenu");
                _inMainMenu = false;
            });
        }
        else
        {
            _mainMenu.StartBtn.onClick.AddListener(StartGame);
        }

        _tryAgainButton.onClick.AddListener(() => TryAgain());
        _victoryMenu.BackBtn.onClick.AddListener(ReturnToMainMenu);
        _loseMenu.BackBtn.onClick.AddListener(ReturnToMainMenu);
        _participationMenu.BackBtn.onClick.AddListener(ReturnToMainMenu);
        _openRestButton.onClick.AddListener(() => OpenRestScreen());
        _closeRestButton.onClick.AddListener(() => CloseRestScreen(false));
        _closeRestButtonAutomatic.onClick.AddListener(() => CloseRestScreen(true));
    }

    private void InstantiateCards()
    {
        Debug.Log("Instantiate");

        for (int i = 0; i < _config.cardPairs.Length; i++)
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

        // Priorizar o maior número de colunas possível
        int numberOfColumns = 3; // Começamos assumindo todas as cartas em uma linha
        int numberOfRows = 4;

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
        });
    }

    private void WinGame(string prizeName = null)
    {
        SoundSystem.Instance.Play("Win");

        if (!string.IsNullOrEmpty(prizeName))
        {
            _victoryMenu.SecondaryText = $"Você ganhou um <b>{prizeName}</b>";
            _gameMenu.OpenMenu("VictoryMenu");
        }
        else
        {
            prizeName = "Nenhum";
            _gameMenu.OpenMenu("ParticipationMenu");
        }

        FillJObectByResult(_secondTry, true, prizeName);
    }

    private void LoseGame()
    {
        SoundSystem.Instance.Play("Fail");

        FillJObectByResult(_secondTry, true);

        _gameMenu.OpenMenu("LoseMenu");
    }

    private void FillJObectByResult(bool triedAgain, bool won, string prizeName = "Nenhum")
    {
        string triedAgainText = triedAgain ? "Sim" : "Não";

        if (triedAgain)
        {
            if (won)
            {
                AppManager.Instance.DataSync.AddDataToJObject("ganhou", "sim");
                AppManager.Instance.DataSync.AddDataToJObject("brinde", prizeName);
                AppManager.Instance.DataSync.AddDataToJObject("custom3", triedAgainText);
            }
            else
            {
                AppManager.Instance.DataSync.AddDataToJObject("ganhou", "não");
                AppManager.Instance.DataSync.AddDataToJObject("brinde", "nenhum");
                AppManager.Instance.DataSync.AddDataToJObject("custom3", triedAgainText);
            }
        }
        else
        {
            if (won)
            {
                AppManager.Instance.DataSync.AddDataToJObject("ganhou", "sim");
                AppManager.Instance.DataSync.AddDataToJObject("brinde", prizeName);
                AppManager.Instance.DataSync.AddDataToJObject("custom3", triedAgainText);
            }
            else
            {
                AppManager.Instance.DataSync.AddDataToJObject("ganhou", "não");
                AppManager.Instance.DataSync.AddDataToJObject("brinde", "nenhum");
                AppManager.Instance.DataSync.AddDataToJObject("custom3", triedAgainText);
            }
        }
    }

    private void TryAgain()
    {
        _secondTry = true;

        _tryAgainButton.gameObject.SetActive(false);

        StartGame();
    }

    private void ReturnToMainMenu()
    {
        _gameMenu.OpenMenu("MainMenu");

        _inMainMenu = true;

        RDStationManager.Instance.SendConversion(AppManager.Instance.DataSync.CurrentLead);

        AppManager.Instance.DataSync.SaveLeads();
    }

    private void RestScreenClick()
    {
        //_restClicks++;

        //if (_restClicks == 4)
        //{
        //    _restClicks = 0;
            OpenRestScreen();
        //}
    }

    private void OpenRestScreen(bool automatic = false)
    {
        if (automatic)
            _closeRestButtonAutomatic.gameObject.SetActive(true);
        else
            _closeRestButtonAutomatic.gameObject.SetActive(false);

        _gameMenu.OpenMenu("RestMenu");

        AppManager.Instance.OpenMenu("CloseAll");

        _inMainMenu = false;

        _restVideo.Play();
    }

    private void CloseRestScreen(bool fullClick)
    {
        if (fullClick)
        {
            _restClicks = 0;

            _gameMenu.OpenMenu("MainMenu");
            _restVideo.Stop();

            _closeRestButtonAutomatic.gameObject.SetActive(false);
            _inMainMenu = true;
            return;
        }
        
        _restClicks++;

        if (_restClicks == 4)
        {
            _restClicks = 0;

            _gameMenu.OpenMenu("MainMenu");
            _restVideo.Stop();
            _inMainMenu = true;
        }
    }
}