
// Optional: Custom ranking entry component
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class RankingEntry : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI dateText;

    public void SetupEntry(Dictionary<string, string> data, int rank)
    {
        if (rankText != null) rankText.text = $"{rank}";
        if (nameText != null) nameText.text = data.GetValueOrDefault("nome", data.GetValueOrDefault("name", "Unknown"));
        if (scoreText != null) scoreText.text = data.GetValueOrDefault("pontos", "0");
        if (dateText != null) dateText.text = data.GetValueOrDefault("tempo", "");
    }
}

// New configurable ranking entry component that works with RankingDataID
public class ConfigurableRankingEntry : MonoBehaviour
{
    [Header("Data Field UI Elements")]
    public List<RankingDataFieldUI> dataFieldUIs = new List<RankingDataFieldUI>();

    [Header("Special Elements")]
    public TextMeshProUGUI rankText;

    public void SetupEntry(Dictionary<string, string> data, int rank, List<RankingDataField> fields)
    {
        // Set rank
        if (rankText != null)
            rankText.text = $"{rank}";

        // Setup data fields based on configuration
        var orderedFields = fields.Where(f => f.showInRanking).OrderBy(f => f.displayOrder).ToList();

        for (int i = 0; i < dataFieldUIs.Count && i < orderedFields.Count; i++)
        {
            var fieldConfig = orderedFields[i];
            var uiElement = dataFieldUIs[i];

            if (uiElement.textComponent != null)
            {
                string value = data.GetValueOrDefault(fieldConfig.dataID.ToString(), "");
                uiElement.textComponent.text = value;
            }

            // Set field label if available
            if (uiElement.labelComponent != null)
            {
                uiElement.labelComponent.text = fieldConfig.displayName;
            }

            // Show/hide based on configuration
            if (uiElement.containerObject != null)
            {
                uiElement.containerObject.SetActive(!string.IsNullOrEmpty(data.GetValueOrDefault(fieldConfig.dataID.ToString(), "")));
            }
        }

        // Hide unused UI elements
        for (int i = orderedFields.Count; i < dataFieldUIs.Count; i++)
        {
            if (dataFieldUIs[i].containerObject != null)
                dataFieldUIs[i].containerObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class RankingDataFieldUI
{
    public RankingDataID expectedDataID;
    public UnityEngine.UI.Text textComponent;
    public UnityEngine.UI.Text labelComponent;
    public GameObject containerObject;
}