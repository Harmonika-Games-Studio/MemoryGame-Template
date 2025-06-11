using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Harmonika.Tools;
using TMPro;

[System.Serializable]
public class Serialization<T>
{
    public T Data;

    public Serialization(T data)
    {
        Data = data;
    }
}

public enum RankingDataID
{
    nome,
    pontos,
    tempo,
    date,
    custom1,
    custom2,
    custom3,
    custom4,
    custom5,
    custom6,
    custom7,
    custom8
}

[System.Serializable]
public class RankingDataField
{
    public RankingDataID dataID;
    public string displayName;
    public bool showInRanking = true;
    public int displayOrder = 0;

    public RankingDataField(RankingDataID id, string name, bool show = true, int order = 0)
    {
        dataID = id;
        displayName = name;
        showInRanking = show;
        displayOrder = order;
    }
}

public class RankingManager : MonoBehaviour
{
    [Header("Ranking Settings")]
    public bool dailyRanking = false;
    public string rankingFileName = "ranking_data.json";
    public string resetSuffix = "_reset";

    [Header("Display Settings")]
    [Range(1, 100)]
    public int maxEntriesToShow = 10;

    [Header("Data Configuration")]
    public List<RankingDataField> rankingFields = new List<RankingDataField>();
    public RankingDataID sortByField = RankingDataID.pontos;
    public bool sortDescending = true;

    [Header("Secondary Sort Configuration")]
    public RankingDataID sortThenByField = RankingDataID.nome;
    public bool sortThenByDescending = false;
    public bool useThenBySort = false;

    private string rankingFilePath;
    private string resetFilePath;

    // UI References (assign these in inspector)
    [Header("UI References")]
    public Transform rankingContainer;
    public GameObject rankingEntryPrefab;
    public UnityEngine.UI.Button resetButton;
    public UnityEngine.UI.Toggle dailyToggle;
    public UnityEngine.UI.Toggle allTimeToggle;

    public static RankingManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializePaths();
        InitializeDefaultFields();
        SetupUI();
    }

    private void InitializeDefaultFields()
    {
        if (rankingFields.Count == 0)
        {
            // Initialize with default ranking fields
            rankingFields.Add(new RankingDataField(RankingDataID.nome, "Player", true, 0));
            rankingFields.Add(new RankingDataField(RankingDataID.pontos, "Score", true, 1));
            rankingFields.Add(new RankingDataField(RankingDataID.tempo, "Date", true, 3));
        }
    }

    private void InitializePaths()
    {
        rankingFilePath = Path.Combine(Application.persistentDataPath, rankingFileName);
        resetFilePath = Path.Combine(Application.persistentDataPath, rankingFileName.Replace(".json", resetSuffix + ".json"));
    }

    private void SetupUI()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetRanking);

        if (dailyToggle != null)
        {
            dailyToggle.isOn = dailyRanking;
            dailyToggle.onValueChanged.AddListener(OnDailyToggleChanged);
        }

        if (allTimeToggle != null)
        {
            allTimeToggle.isOn = !dailyRanking;
            allTimeToggle.onValueChanged.AddListener(OnAllTimeToggleChanged);
        }
    }

    /// <summary>
    /// Sets the maximum number of entries to display in the ranking
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to show (minimum 1)</param>
    public void SetMaxEntriesToShow(int maxEntries)
    {
        maxEntriesToShow = Mathf.Max(1, maxEntries);
        ShowRanking(); // Refresh the display with new limit
    }

    /// <summary>
    /// Gets the current maximum number of entries to display
    /// </summary>
    /// <returns>Current maximum entries limit</returns>
    public int GetMaxEntriesToShow()
    {
        return maxEntriesToShow;
    }

    /// <summary>
    /// Saves ranking data to the ranking file
    /// </summary>
    /// <param name="data">JObject containing the ranking data</param>
    public void SaveRankingData(JObject data)
    {
        try
        {
            // Convert JObject to Dictionary<string, string>
            var fields = new Dictionary<string, string>();

            foreach (var property in data.Properties())
            {
                fields[property.Name] = property.Value.ToString();
            }

            fields["dataHora"] = DateTime.Now.ToString("yyyy-MM-dd");

            SaveLocalData(rankingFilePath, fields);

            // Refresh ranking display if it's currently shown
            if (rankingContainer != null)
            {
                ShowRanking();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving ranking data: {e.Message}");
        }
    }

    /// <summary>
    /// Shows the ranking based on current settings (daily or all-time)
    /// </summary>
    public void ShowRanking()
    {
        ShowRanking(dailyRanking);
    }

    /// <summary>
    /// Shows the ranking with specified time filter
    /// </summary>
    /// <param name="todayOnly">If true, shows only today's rankings</param>
    public void ShowRanking(bool todayOnly)
    {
        try
        {
            var rankingData = LoadLocalData(rankingFilePath);

            if (rankingData == null || rankingData.Count == 0)
            {
                Debug.Log("No ranking data found");
                ClearRankingDisplay();
                return;
            }

            // Filter by date if needed
            if (todayOnly)
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                rankingData = rankingData.Where(entry =>
                    entry.ContainsKey("dataHora") && entry["dataHora"] == today).ToList();
            }

            // Sort ranking data based on configured sort field
            if (rankingData.Any(entry => entry.ContainsKey(sortByField.ToString())))
            {
                rankingData = SortRankingData(rankingData);
            }

            // Limit the number of entries to show
            if (rankingData.Count > maxEntriesToShow)
            {
                rankingData = rankingData.Take(maxEntriesToShow).ToList();
                Debug.Log($"Limiting ranking display to top {maxEntriesToShow} entries");
            }

            DisplayRankingData(rankingData, todayOnly);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error showing ranking: {e.Message}");
        }
    }

    /// <summary>
    /// Resets the ranking by renaming the current file
    /// </summary>
    public void ResetRanking()
    {
        try
        {
            if (File.Exists(rankingFilePath))
            {
                // If reset file already exists, delete it first
                if (File.Exists(resetFilePath))
                {
                    File.Delete(resetFilePath);
                }

                // Rename current ranking file
                File.Move(rankingFilePath, resetFilePath);
                Debug.Log($"Ranking reset. Old data moved to: {resetFilePath}");
            }
            else
            {
                Debug.Log("No ranking file to reset");
            }

            // Clear current display
            ClearRankingDisplay();

            // Show updated (empty) ranking
            ShowRanking();

            PopupManager.Instance.InvokeConfirmDialog("Ranking limpo com sucesso!", "OK!", true);
        }
        catch (Exception e)
        {
            PopupManager.Instance.InvokeConfirmDialog("Ranking vazio!", "OK!", true);

            Debug.LogError($"Error resetting ranking: {e.Message}");
        }
    }

    /// <summary>
    /// Gets a specific data value from ranking data using RankingDataID
    /// </summary>
    /// <param name="data">The ranking data dictionary</param>
    /// <param name="dataID">The data ID to retrieve</param>
    /// <returns>The data value or empty string if not found</returns>
    public string GetRankingData(Dictionary<string, string> data, RankingDataID dataID)
    {
        return data.GetValueOrDefault(dataID.ToString(), "");
    }

    /// <summary>
    /// Sets a specific data value in ranking data using RankingDataID
    /// </summary>
    /// <param name="data">The ranking data dictionary</param>
    /// <param name="dataID">The data ID to set</param>
    /// <param name="value">The value to set</param>
    public void SetRankingData(Dictionary<string, string> data, RankingDataID dataID, string value)
    {
        data[dataID.ToString()] = value;
    }

    /// <summary>
    /// Gets the LeadID equivalent from RankingDataID (for compatibility)
    /// </summary>
    /// <param name="rankingID">The ranking data ID</param>
    /// <returns>The corresponding LeadID</returns>
    public LeadID GetLeadID(RankingDataID rankingID)
    {
        // Map common fields or return a custom field
        switch (rankingID)
        {
            case RankingDataID.nome:
                return LeadID.nome;
            case RankingDataID.custom1:
                return LeadID.custom1;
            case RankingDataID.custom2:
                return LeadID.custom2;
            case RankingDataID.custom3:
                return LeadID.custom3;
            case RankingDataID.custom4:
                return LeadID.custom4;
            case RankingDataID.custom5:
                return LeadID.custom5;
            default:
                return LeadID.custom1; // Default fallback
        }
    }

    /// <summary>
    /// Sorts ranking data based on the configured sort field and optional secondary sort
    /// </summary>
    private List<Dictionary<string, string>> SortRankingData(List<Dictionary<string, string>> data)
    {
        var sortField = sortByField.ToString();
        var secondSortField = sortThenByField.ToString();

        if (sortDescending)
        {
            var firstSort = data.OrderByDescending(entry =>
            {
                var value = entry.GetValueOrDefault(sortField, "0");
                if (float.TryParse(value, out float numValue))
                    return numValue;
                return 0f; // Non-numeric values get 0
            });

            if (useThenBySort && !string.IsNullOrEmpty(secondSortField))
            {
                if (sortThenByDescending)
                {
                    return firstSort.ThenByDescending(entry =>
                    {
                        var value = entry.GetValueOrDefault(secondSortField, "0");
                        if (float.TryParse(value, out float numValue))
                            return numValue;
                        return 0f;
                    }).ThenByDescending(entry => entry.GetValueOrDefault(secondSortField, "")).ToList();
                }
                else
                {
                    return firstSort.ThenBy(entry =>
                    {
                        var value = entry.GetValueOrDefault(secondSortField, "0");
                        if (float.TryParse(value, out float numValue))
                            return numValue;
                        return float.MaxValue;
                    }).ThenBy(entry => entry.GetValueOrDefault(secondSortField, "")).ToList();
                }
            }
            else
            {
                return firstSort.ThenBy(entry => entry.GetValueOrDefault(sortField, "")).ToList();
            }
        }
        else
        {
            var firstSort = data.OrderBy(entry =>
            {
                var value = entry.GetValueOrDefault(sortField, "0");
                if (float.TryParse(value, out float numValue))
                    return numValue;
                return float.MaxValue; // Non-numeric values go to end
            });

            if (useThenBySort && !string.IsNullOrEmpty(secondSortField))
            {
                if (sortThenByDescending)
                {
                    return firstSort.ThenByDescending(entry =>
                    {
                        var value = entry.GetValueOrDefault(secondSortField, "0");
                        if (float.TryParse(value, out float numValue))
                            return numValue;
                        return 0f;
                    }).ThenByDescending(entry => entry.GetValueOrDefault(secondSortField, "")).ToList();
                }
                else
                {
                    return firstSort.ThenBy(entry =>
                    {
                        var value = entry.GetValueOrDefault(secondSortField, "0");
                        if (float.TryParse(value, out float numValue))
                            return numValue;
                        return float.MaxValue;
                    }).ThenBy(entry => entry.GetValueOrDefault(secondSortField, "")).ToList();
                }
            }
            else
            {
                return firstSort.ThenBy(entry => entry.GetValueOrDefault(sortField, "")).ToList();
            }
        }
    }

    /// <summary>
    /// Gets the current ranking data
    /// </summary>
    /// <param name="todayOnly">If true, returns only today's data</param>
    /// <param name="limitEntries">If true, limits results to maxEntriesToShow</param>
    /// <returns>List of ranking entries</returns>
    public List<Dictionary<string, string>> GetRankingData(bool todayOnly = false, bool limitEntries = false)
    {
        var data = LoadLocalData(rankingFilePath);

        if (data != null && todayOnly)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            data = data.Where(entry =>
                entry.ContainsKey("date") && entry["date"] == today).ToList();
        }

        if (data != null && limitEntries && data.Count > maxEntriesToShow)
        {
            // Sort before limiting if we have sort criteria
            if (data.Any(entry => entry.ContainsKey(sortByField.ToString())))
            {
                data = SortRankingData(data);
            }
            data = data.Take(maxEntriesToShow).ToList();
        }

        return data ?? new List<Dictionary<string, string>>();
    }

    private void SaveLocalData(string path, Dictionary<string, string> fields)
    {
        var localData = LoadLocalData(path) ?? new List<Dictionary<string, string>>();
        localData.Add(fields);
        var json = JsonConvert.SerializeObject(new Serialization<List<Dictionary<string, string>>>(localData));
        File.WriteAllText(path, json);
        Debug.Log("Campos salvos localmente: " + JsonConvert.SerializeObject(fields) + "\nSaved on " + path);
    }

    private List<Dictionary<string, string>> LoadLocalData(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<Serialization<List<Dictionary<string, string>>>>(json).Data;
            }
        }
        return new List<Dictionary<string, string>>();
    }

    private void DisplayRankingData(List<Dictionary<string, string>> rankingData, bool todayOnly)
    {
        ClearRankingDisplay();

        if (rankingContainer == null)
        {
            Debug.LogWarning("Ranking container not assigned!");
            return;
        }

        Debug.Log($"Displaying {rankingData.Count} ranking entries ({(todayOnly ? "Today" : "All Time")}) - Limited to {maxEntriesToShow} max");

        for (int i = 0; i < rankingData.Count; i++)
        {
            var entry = rankingData[i];
            CreateRankingEntry(entry, i + 1);
        }
    }

    private void CreateRankingEntry(Dictionary<string, string> entryData, int rank)
    {
        if (rankingEntryPrefab == null || rankingContainer == null)
            return;

        GameObject entryObj = Instantiate(rankingEntryPrefab, rankingContainer);

        // Get the configurable ranking entry component
        var configurableEntry = entryObj.GetComponent<ConfigurableRankingEntry>();
        if (configurableEntry != null)
        {
            configurableEntry.SetupEntry(entryData, rank, rankingFields);
            return;
        }

        // Fallback: Try to find and populate common UI elements
        var rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        var dateText = entryObj.transform.Find("DateText")?.GetComponent<TextMeshProUGUI>();

        if (rankText != null) rankText.text = $"{rank}";
        if (nameText != null) nameText.text = GetRankingData(entryData, RankingDataID.nome);
        if (scoreText != null) scoreText.text = GetRankingData(entryData, RankingDataID.pontos);
        if (dateText != null) dateText.text = GetRankingData(entryData, RankingDataID.tempo);

        // Legacy support for old RankingEntry component
        var customRankingEntry = entryObj.GetComponent<RankingEntry>();
        if (customRankingEntry != null)
        {
            customRankingEntry.SetupEntry(entryData, rank);
        }
    }

    private void ClearRankingDisplay()
    {
        if (rankingContainer == null) return;

        foreach (Transform child in rankingContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void OnDailyToggleChanged(bool isOn)
    {
        if (isOn)
        {
            dailyRanking = true;
            if (allTimeToggle != null) allTimeToggle.SetIsOnWithoutNotify(false);
            ShowRanking(true);
        }
    }

    private void OnAllTimeToggleChanged(bool isOn)
    {
        if (isOn)
        {
            dailyRanking = false;
            if (dailyToggle != null) dailyToggle.SetIsOnWithoutNotify(false);
            ShowRanking(false);
        }
    }

    // Public methods for external control
    public void SetDailyRanking(bool daily)
    {
        dailyRanking = daily;
        if (dailyToggle != null) dailyToggle.SetIsOnWithoutNotify(daily);
        if (allTimeToggle != null) allTimeToggle.SetIsOnWithoutNotify(!daily);
        ShowRanking();
    }

    public bool IsDailyRanking()
    {
        return dailyRanking;
    }
}