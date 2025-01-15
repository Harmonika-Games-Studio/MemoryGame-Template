using Harmonika.Menu;
using Harmonika.Tools;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class JsonDeserializedConfig
{
    public string gameName;
    public string primaryColor;
    public string cardBack;
    public string[] cardsList;
    public bool useLeads;
    public StorageItemConfig[] storageItems;
    public LeadDataConfig[] leadDataConfig;

    // Construtor padrão necessário para a desserialização
    [JsonConstructor]
    public JsonDeserializedConfig(
        string gameName = "",
        string primaryColor = "",
        string cardBack = "",
        string[] cardsList = null,
        StorageItemConfig[] storageItems = null,
        LeadDataConfig[] leadDataConfig = null)
    {
        this.gameName = gameName;
        this.primaryColor = primaryColor;
        this.cardBack = cardBack;
        this.cardsList = cardsList ?? Array.Empty<string>();
        this.storageItems = storageItems ?? Array.Empty<StorageItemConfig>();
        this.leadDataConfig = leadDataConfig ?? Array.Empty<LeadDataConfig>();
    }
}

public class MemoryGame : MonoBehaviour
{
    [SerializeField] private MemoryGameWebConfig _config;
    
    [Header("References")]
    [SerializeField] private Cronometer _cronometer;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Menus")]
    [SerializeField] private CustomStartMenu _startMenu;
    [SerializeField] private CustomCollectLeadsMenu _collectLeadsMenu;
    [SerializeField] private CustomVictoryMenu _victoryMenu;
    [SerializeField] private CustomParticipationMenu _participationMenu;
    [SerializeField] private CustomLoseMenu _loseMenu;

    [Header("Visuals")]
    [SerializeField] private Image _gameBackground;

    [Header("WebVersion")]
    [SerializeField] private CustomWebAutoLeadForm _webAutoLeadForm;

    private int _revealedPairs;
    private int _remainingTime;
    private bool _canClick = true;
    private float _startTime;
    private MenuManager _gameMenu;
    private RectTransform _gridLayoutRect;

    private MemoryGameCard _lastClickedCard;
    private List<MemoryGameCard> _cardsList = new List<MemoryGameCard>();

    #region Properties

    public MemoryGameWebConfig Config { get => _config; }

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

    private void Start()
    {
        TrySetupGameConfigFromWebData();
    }

    private void Update()
    {
       if (Input.GetKeyDown(KeyCode.T))
            WebGLNetworking.Instance.ReceiveJson(WebGLNetworking.Instance.TestJson);
    }

    public IEnumerator StartGame()
    {
        _startTime = Time.time;
         _cronometer.totalTimeInSeconds = PlayerPrefs.GetInt("GameTime");
        _cronometer.StartTimer();

        InstantiateCards();
        AdjustGridLayout();
        ShuffleCards();
        yield return new WaitForSeconds(_config.memorizationTime);

        foreach (var card in _cardsList)
        {
            card.RotateCardDown();
        }
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
        _startMenu.StartGameButton.onClick.RemoveAllListeners();
        _collectLeadsMenu.ContinueGameButton.onClick.RemoveAllListeners();
        _victoryMenu.BackButton.onClick.RemoveAllListeners();
        _loseMenu.BackButton.onClick.RemoveAllListeners();
        _participationMenu.BackButton.onClick.RemoveAllListeners();

        if (AppManager.Instance.gameConfig.useLeads)
        {
            _startMenu.AddStartGameButtonListener(() => _gameMenu.OpenMenu("CollectLeadsMenu"));
            _startMenu.AddStartGameButtonListener(() => _collectLeadsMenu.ClearAllFields());
            _collectLeadsMenu.AddContinueGameButtonListener(() => _gameMenu.CloseMenus()); 
            _collectLeadsMenu.AddContinueGameButtonListener(() => StartCoroutine(StartGame()));
            _collectLeadsMenu.AddBackButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        }
        else
        {
            _startMenu.AddStartGameButtonListener(() => _gameMenu.CloseMenus());
            _startMenu.AddStartGameButtonListener(() => StartCoroutine(StartGame()));
        }

        _victoryMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _loseMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
        _participationMenu.AddBackToMainMenuButtonListener(() => _gameMenu.OpenMenu("MainMenu"));
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
        int numberOfColumns = totalCards; // Começamos assumindo todas as cartas em uma linha
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
        }

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
        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);

    }

    private void EndGame(bool win, string prizeName = null)
    {
        _cronometer.EndTimer();
        float tempo = Time.time - _startTime;
        AppManager.Instance.DataSync.AddDataToJObject("tempo", tempo);
        AppManager.Instance.DataSync.AddDataToJObject("pontos", (int)Math.Floor(_config.gameTime - tempo));

        InvokeUtility.Invoke(() =>
        {
            if (win) WinGame(prizeName);
            else LoseGame();

            foreach (var card in _cardsList)
            {
                Destroy(card.gameObject);
            }
        _cardsList.Clear();
        _revealedPairs = 0;

        AppManager.Instance.DataSync.SendLeads();
        }, 1f);
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
        AppManager.Instance.DataSync.AddDataToJObject("brinde", prizeName);
    }

    private void LoseGame()
    {
        SoundSystem.Instance.Play("Fail");

        AppManager.Instance.DataSync.AddDataToJObject("ganhou", "não");
        AppManager.Instance.DataSync.AddDataToJObject("brinde", "nenhum");

        _gameMenu.OpenMenu("LoseMenu");
    }

    #region Networking
    private void TrySetupGameConfigFromWebData()
    {
        LoadingScript.Instance.Loading = true;

        if (string.IsNullOrEmpty(WebGLNetworking.Instance.jsonData))
        {
            Debug.Log("WebGLNetworking -> TryStartConnection: Tentando conexão");
            Invoke(nameof(TrySetupGameConfigFromWebData), 1f);
        }
        else
        {
            Debug.Log("WebGLNetworking -> TryStartConnection: Conexão Concluída, Iniciando download");
            Debug.Log("WebGLNetworking -> SetupGameConfigFromWeb: Baixando Imagens");

            try
            {
                JsonDeserializedConfig data = JsonUtility.FromJson<JsonDeserializedConfig>(WebGLNetworking.Instance.jsonData);
                Debug.Log("WebGLNetworking -> SetupGameConfigFromWeb: Desserialização com JsonUtility bem-sucedida!");
                StartCoroutine(SetupGameConfigFromWebData(data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"WebGLNetworking -> SetupGameConfigFromWeb: Erro na desserialização: {ex.Message}");
            }
        }
    }

    IEnumerator SetupGameConfigFromWebData(JsonDeserializedConfig data)
    {
        _config.storageItems = data.storageItems;
        _config.useLeads = data.useLeads;
        _config.gameName = data.gameName;
        _config.primaryColor = data.primaryColor.HexToColor();
        _config.cardPairs = new Sprite[data.cardsList.Length];

        if (data.leadDataConfig == null || data.leadDataConfig.Length == 0)
        {
            _config.useLeads = false;
        }
        else
        {
            _config.useLeads = true;
        }

        AppManager.Instance.gameConfig = _config;

        yield return StartCoroutine(WebGLNetworking.Instance.DownloadImageRoutine(data.cardBack, (sprite) => _config.cardBack = sprite));
        for (int i = 0; i < _config.cardPairs.Length; i++)
        {
            yield return StartCoroutine(WebGLNetworking.Instance.DownloadImageRoutine(data.cardsList[i], (sprite) => _config.cardPairs[i] = sprite));
        }

        Debug.Log("isNull: " + data.leadDataConfig == null);
        Debug.Log("LeadDataConfig: " + data.leadDataConfig.Length);
        Debug.Log("useLeads: " + AppManager.Instance.useLeads);
        _webAutoLeadForm.leadDataConfig = data.leadDataConfig;
        _webAutoLeadForm.InstantiateLeadboxes();
        _webAutoLeadForm.submitButton.gameObject.SetActive(_webAutoLeadForm.CheckInputsFilled());
        SetupButtons();
        SetupGameConfigFromScriptable();
        AppManager.Instance.Storage.Setup();
        LoadingScript.Instance.Loading = false;
    }
    #endregion


    public void SetupGameConfigFromScriptable()
    {
        AppManager.Instance.gameConfig = _config;
        AppManager.Instance.ApplyScriptableConfig();

        _gameBackground.color = _config.primaryColor;
        _cronometer.image.color = Config.tertiaryColor;

        _startMenu.ChangeVisualIdentity(_config);
        _collectLeadsMenu.ChangeVisualIdentity(_config);
        _victoryMenu.ChangeVisualIdentity(_config);
        _participationMenu.ChangeVisualIdentity(_config);
        _loseMenu.ChangeVisualIdentity(_config);
    }
}