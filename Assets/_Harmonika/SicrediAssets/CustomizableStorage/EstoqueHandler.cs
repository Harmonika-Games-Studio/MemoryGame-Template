using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Globalization;
using System.Text;

public class EstoqueHandler : MonoBehaviour {


    [System.Serializable]
    public class StorageItemValues
    {
        public string name;
        public int initialValue;
        public Sprite image;
        public string displayName; // Display name that can be customized
    }

    public static EstoqueHandler instance;
    private int storageClicks;
    private bool isUsed = true;
    private const int ITEMS_PER_PAGE = 6;
    private const float STORAGE_CLICK_RESET_TIME = 4f;
    private const string STORAGE_USED_KEY = "isStorageUsed";
    private const string FIRST_TIME_ESTOQUE_KEY = "FirstTimeEstoque";
    private const string NAO_FOI_DESSA_VEZ_KEY = "naoFoiDessaVezChance";
    private const string TENTE_OUTRA_VEZ_KEY = "tenteOutraVezChance";
    private const string NAO_FOI_DESSA_VEZ_TEXT = "Não foi dessa vez";
    private const string TENTE_OUTRA_VEZ_TEXT = "Tente outra vez";
    private const string ITEM_NAME_SUFFIX = "_displayName";

    [Header("UI Storage")]
    public GameObject pnlStorage;
    public GameObject pnlEmptyStorage;
    public Toggle isStorageOnToggle;
    public Button saveStorageButton;
    public Button previousPageButton;
    public Button nextPageButton;
    public Transform storageParent;
    public List<SicrediStorageItem> _storageElements = new List<SicrediStorageItem>();

    public TMP_InputField naoFoiDessaVezInput;
    public TMP_InputField tenteOutraVezInput;

    public float naoFoiDessaVezChance;
    public float tenteOutraVezChance;

    [Header("UI Empty")]
    public Button emptyScreenExitButton;

    [Header("UI Outside")]
    public Button openStorageButton;

    [Header("Prefab")]
    public GameObject storageItemPrefab;

    [Header("Dados do estoque")]
    public List<StorageItemValues> storageItems;

    public Action OnSetupStorage;

    private int storageCurPage;
    private Coroutine storageClickResetCoroutine;
    private Dictionary<string, string> itemDisplayNames = new Dictionary<string, string>();

    #region Propeties
    public List<SicrediStorageItem> ItemsList {
        get => _storageElements;
    }

    public int InventoryCount
    {
        get
        {
            int totalValue = 0;
            foreach (var item in _storageElements)
            {
                totalValue += item.Quantity;
            }
            return totalValue;
        }
    }
    #endregion

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        SetupStorageCall();
    }

    private void Start()
    {
        LoadStoredProbabilities();
        LoadStoredItemNames();
        SetupUIListeners();
        SetupToggle();
    }

    private void LoadStoredProbabilities()
    {
        naoFoiDessaVezChance = PlayerPrefs.GetFloat(NAO_FOI_DESSA_VEZ_KEY, naoFoiDessaVezChance);
        tenteOutraVezChance = PlayerPrefs.GetFloat(TENTE_OUTRA_VEZ_KEY, tenteOutraVezChance);

        naoFoiDessaVezInput.text = naoFoiDessaVezChance.ToString();
        tenteOutraVezInput.text = tenteOutraVezChance.ToString();
    }

    private void LoadStoredItemNames()
    {
        itemDisplayNames.Clear();

        // Load any custom display names that have been saved
        foreach (var item in storageItems)
        {
            string savedName = PlayerPrefs.GetString(item.name + ITEM_NAME_SUFFIX, item.displayName ?? item.name);
            itemDisplayNames[item.name] = savedName;
        }
    }

    private void SetupUIListeners()
    {
        saveStorageButton.onClick.AddListener(SaveStorage);
        emptyScreenExitButton.onClick.AddListener(() => pnlEmptyStorage.SetActive(false));
        openStorageButton.onClick.AddListener(OpenStorage);

        nextPageButton.onClick.AddListener(() => ChangePages(1));
        previousPageButton.onClick.AddListener(() => ChangePages(-1));

        isStorageOnToggle.onValueChanged.AddListener(ToggleStorageUsed);
    }

    public void SetupStorageCall()
    {
        StartCoroutine(SetupStorageWait());
    }

    private IEnumerator SetupStorageWait()
    {
        yield return new WaitForSeconds(.3f);

        SetupStorage();
    }

    public string GetSpecificPrize(int i) {
        var item = ItemsList[i];
        item.Quantity -= 1;
        return item.ItemDisplayName;
    }

    public string GetRandomPrize(List<string> restrictions = null) {
        if (InventoryCount <= 0)
            return null;

        List<int> CandidateIndexes = new();

        int validInventoryCount = 0;
        for (int i = 0; i < ItemsList.Count; i++) {
            if (restrictions == null || !restrictions.Contains(ItemsList[i].ItemName)) {
                if (ItemsList[i].ItemChance > 0 && ItemsList[i].Quantity > 0) {
                    Debug.Log($"Prize added as candidate: {ItemsList[i].ItemDisplayName} - Chance {ItemsList[i].ItemChance} - Quantity {ItemsList[i].Quantity}");
                    CandidateIndexes.Add(i);
                    validInventoryCount += (int)ItemsList[i].ItemChance;
                } 
            }
        }

        int randomNumber = UnityEngine.Random.Range(0, validInventoryCount);

        int cumulativeValue = 0;
        for (int i = 0; i < CandidateIndexes.Count; i++) {
            int candidateIndex = CandidateIndexes[i];
            if (ItemsList[candidateIndex].ItemChance > 0 && ItemsList[candidateIndex].Quantity > 0) {
                cumulativeValue += (int)ItemsList[candidateIndex].ItemChance;
                if (randomNumber < cumulativeValue) {
                    return GetSpecificPrize(candidateIndex);
                }
            }
        }

        return null;
    }

    public Sprite GetSpecificItemSprite(int rewardIndex) {

        if (rewardIndex < 0 || rewardIndex >= storageItems.Count) {
            Debug.LogError($"Índice de recompensa inválido: {rewardIndex}");
            return null;
        }

        return storageItems[rewardIndex].image;
    }

    public string GetDisplayNameForItem(string internalName)
    {
        if (itemDisplayNames.TryGetValue(internalName, out string displayName))
        {
            return displayName;
        }
        return internalName; // Default to internal name if no custom display name exists
    }

    private void SetupStorage()
    {
        ClearExistingStorageElements();

        bool isFirstTime = PlayerPrefs.GetInt(FIRST_TIME_ESTOQUE_KEY, 0) == 0;
        bool anyActiveRewards = false;

        for (int i = 0; i < storageItems.Count; i++)
        {
            SicrediStorageItem itemScript = CreateStorageItem(i);
            _storageElements.Add(itemScript);

            if (i >= ITEMS_PER_PAGE)
            {
                itemScript.gameObject.SetActive(false);
            }

            if (UpdateRewardProbabilities(itemScript))
            {
                anyActiveRewards = true;
            }
        }

        if (!anyActiveRewards)
        {
            SetDefaultRewardProbabilities();
        }


        LogRewardProbabilities();

        if (isFirstTime)
        {
            PlayerPrefs.SetInt(FIRST_TIME_ESTOQUE_KEY, 1);
        }

        //MiniGame.instance.ApplyLayout();
    }

    public void RoulleteSetLayout() {
         
        //Update mRewards' gameObject name and text component
        //foreach (MiniGame_Reward reward in MiniGame.instance.mRewards) {
        //    SicrediStorageItem matchingItem = storageElements.FirstOrDefault(element => element.GetCurrentName() == reward.gameObject.name);
        //    if (matchingItem != null) {
        //        string displayName = matchingItem.GetCurrentDisplayName();

        //        GameObject sadFace = reward.transform.Find("Content").Find("SadFace").gameObject;

        //        TextMeshProUGUI textComponent = reward.transform.Find("Content").Find("Text").GetComponent<TextMeshProUGUI>();

        //        textComponent.gameObject.SetActive(true);
        //        sadFace.gameObject.SetActive(false);

        //        if (textComponent != null) {
        //            textComponent.text = displayName;
        //        }

        //        // If the display name is "Não foi dessa vez", do nothing
        //        if (IsNotThisTime(displayName)) {
        //            textComponent.gameObject.SetActive(false);

        //            sadFace.SetActive(true);

        //            matchingItem.SetDisplayName("Não foi dessa vez");

        //            Debug.Log($"Reward '{reward.gameObject.name}' left unchanged because its display name is '{displayName}'");
        //            continue;
        //        }


        //        Debug.Log($"Updated reward: {reward.gameObject.name} (Text: {displayName})");
        //    }
        //}
    }

    private void ClearExistingStorageElements()
    {
        foreach (SicrediStorageItem element in _storageElements)
        {
            if (element != null && element.gameObject != null)
            {
                Destroy(element.gameObject);
            }
        }
        _storageElements.Clear();
    }

    private SicrediStorageItem CreateStorageItem(int index)
    {
        GameObject newItem = Instantiate(storageItemPrefab, storageParent);
        SicrediStorageItem itemScript = newItem.GetComponent<SicrediStorageItem>();

        StorageItemValues itemData = storageItems[index];
        int savedValue = PlayerPrefs.GetInt(itemData.name, itemData.initialValue);
        float savedChance = PlayerPrefs.GetFloat(itemData.name + "_chance", 0f);
        string displayName = GetDisplayNameForItem(itemData.name);

        itemScript.SetupStorageItem(savedValue, itemData.name, savedChance, displayName, index);
        return itemScript;
    }

    private bool UpdateRewardProbabilities(SicrediStorageItem item)
    {
        bool hasActiveReward = false;
        //foreach (MiniGame_Reward reward in MiniGame.instance.mRewards)
        //{
        //    if (reward.name == item.GetCurrentName())
        //    {
        //        reward.Probability = item.GetCurrentValue() > 0 ? item.GetCurrentChance() : 0;

        //        if (reward.Probability > 0)
        //        {
        //            hasActiveReward = true;
        //        }
        //    }
        //    else if (reward.name == NAO_FOI_DESSA_VEZ_TEXT)
        //    {
        //        reward.Probability = naoFoiDessaVezChance;
        //    }
        //}
        return hasActiveReward;
    }

    private void SetDefaultRewardProbabilities()
    {
        //foreach (MiniGame_Reward reward in MiniGame.instance.mRewards)
        //{
        //    if (reward.name == NAO_FOI_DESSA_VEZ_TEXT)
        //    {
        //        reward.Probability = 100;
        //    }
        //}
    }

    private void LogRewardProbabilities()
    {
        //foreach (MiniGame_Reward reward in MiniGame.instance.mRewards)
        //{
        //    Debug.Log($"{reward.name} ({reward.name}) Probability: {reward.Probability}");
        //}
    }

    public void TryOpenStorage()
    {
        if (storageClickResetCoroutine != null)
        {
            StopCoroutine(storageClickResetCoroutine);
        }

        storageClickResetCoroutine = StartCoroutine(ResetStorageClicks());
        storageClicks++;

        if (storageClicks >= 5)
        {
            OpenStorage();
            storageClicks = 0;
        }
    }

    public void OpenStorage()
    {
        SetupStorage();
        storageCurPage = 0;
        ShowStoragePaging();
        pnlEmptyStorage.SetActive(false);
        pnlStorage.SetActive(true);
    }

    private IEnumerator ResetStorageClicks()
    {
        yield return new WaitForSeconds(STORAGE_CLICK_RESET_TIME);
        storageClicks = 0;
        storageClickResetCoroutine = null;
    }

    public void SaveStorage()
    {
        foreach (SicrediStorageItem item in _storageElements)
        {
            item.ChangeValue();
            item.UpdateChance();

            // Save custom display name if it has been changed
            string customName = item.GetCurrentDisplayName();
            if (!string.IsNullOrEmpty(customName))
            {
                PlayerPrefs.SetString(item.GetCurrentName() + ITEM_NAME_SUFFIX, customName);
                itemDisplayNames[item.GetCurrentName()] = customName;
            }
        }

        if (float.TryParse(naoFoiDessaVezInput.text, out float nfValue))
        {
            naoFoiDessaVezChance = nfValue;
            PlayerPrefs.SetFloat(NAO_FOI_DESSA_VEZ_KEY, naoFoiDessaVezChance);
        }

        if (float.TryParse(tenteOutraVezInput.text, out float toValue))
        {
            tenteOutraVezChance = toValue;
            PlayerPrefs.SetFloat(TENTE_OUTRA_VEZ_KEY, tenteOutraVezChance);
        }

        SetupStorage();
        PlayerPrefs.Save();
        pnlStorage.SetActive(false);
    }

    public string GetItemFromStorage()
    {
        List<int> possibleOutcomes = GetPossibleOutcomes();
        if (possibleOutcomes.Count == 0)
        {
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, possibleOutcomes.Count);
        int selectedItemIndex = possibleOutcomes[randomIndex];

        if (selectedItemIndex >= 0 && selectedItemIndex < _storageElements.Count)
        {
            _storageElements[selectedItemIndex].SubtractValue();

            if (IsNotThisTime(_storageElements[selectedItemIndex].GetCurrentDisplayName()))
            {
                return "Não foi dessa vez";
            }

            return _storageElements[selectedItemIndex].GetCurrentDisplayName();
        }

        return null;
    }

    private List<int> GetPossibleOutcomes()
    {
        var list = new List<int>();

        for (int i = 0; i < _storageElements.Count; i++)
        {
            if (_storageElements[i].GetCurrentValue() > 0)
            {
                list.Add(i);
            }
        }

        return list;
    }

    public bool CheckIfStorageEmpty()
    {
        if (!isUsed)
            return false;

        bool isEmpty = _storageElements.All(item => item.GetCurrentValue() <= 0);

        if (isEmpty)
        {
            ShowEmptyStorageScreen();
            return true;
        }

        return false;
    }

    private void ShowEmptyStorageScreen()
    {
        pnlEmptyStorage.SetActive(true);
    }

    private void SetupToggle()
    {
        var isUsedInt = PlayerPrefs.GetInt(STORAGE_USED_KEY, 0);
        isStorageOnToggle.isOn = isUsedInt == 0;

        // Ensure the toggle correctly reflects the actual state
        ToggleStorageUsed(isStorageOnToggle.isOn);
    }

    private void ToggleStorageUsed(bool yesOrNo)
    {
        isUsed = yesOrNo;
        storageParent.GetComponent<CanvasGroup>().interactable = isUsed;
        PlayerPrefs.SetInt(STORAGE_USED_KEY, isUsed ? 0 : 1);
    }

    public void SetChanceToZero(string name)
    {
        //foreach (MiniGame_Reward reward in MiniGame.instance.mRewards)
        //{
        //    if (reward.name == name)
        //    {
        //        reward.Probability = 0;
        //    }
        //}
        UpdateAllItemChances();
    }

    private void UpdateAllItemChances()
    {
        float totalValue = 0;

        // Calculate total value first
        foreach (SicrediStorageItem item in _storageElements)
        {
            if (item.GetCurrentValue() > 0 && item.GetCurrentChance() > 0)
            {
                totalValue += item.GetCurrentValue();
            }
        }

        if (totalValue > 0)
        {
            // Update chances based on total value
            foreach (SicrediStorageItem item in _storageElements)
            {
                if (item.GetCurrentValue() > 0 && item.GetCurrentChance() > 0)
                {
                    float chance = (item.GetCurrentValue() / totalValue) * 100f;
                    item.SetChance(chance);
                }
            }
        }

        //MiniGame.instance.ApplyLayout();
    }

    private void ShowStoragePaging()
    {
        var pageStart = storageCurPage * ITEMS_PER_PAGE;
        var pageEnd = Mathf.Min(pageStart + ITEMS_PER_PAGE, _storageElements.Count);

        DeactivateAllItems();

        for (int i = pageStart; i < pageEnd; i++)
        {
            if (i < _storageElements.Count)
            {
                _storageElements[i].gameObject.SetActive(true);
            }
        }

        nextPageButton.gameObject.SetActive(pageEnd < _storageElements.Count);
        previousPageButton.gameObject.SetActive(storageCurPage > 0);
    }

    private void DeactivateAllItems()
    {
        foreach (SicrediStorageItem element in _storageElements)
        {
            if (element != null && element.gameObject != null)
            {
                element.gameObject.SetActive(false);
            }
        }
    }

    private void ChangePages(int direction)
    {
        int newPage = storageCurPage + direction;
        int maxPage = Mathf.CeilToInt(_storageElements.Count / (float)ITEMS_PER_PAGE) - 1;

        if (newPage >= 0 && newPage <= maxPage)
        {
            storageCurPage = newPage;
            ShowStoragePaging();
        }
    }

    public static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Normalize to decompose accents, then remove non-ASCII characters
        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().ToLower().Trim();
    }

    public static bool IsNotThisTime(string input)
    {
        string[] validVariations = { "não foi dessa vez", "nao foi dessa vez" };
        string normalizedInput = NormalizeString(input);

        return validVariations.Contains(normalizedInput);
    }

}
