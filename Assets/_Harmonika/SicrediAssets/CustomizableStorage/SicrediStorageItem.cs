using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

public class SicrediStorageItem : MonoBehaviour
{

    [Header("UI Item")]
    public TMP_InputField nameInputField;
    public TMP_InputField valueInputField;
    public TMP_InputField chanceInputField;
    public TMP_Text indexText;

    [SerializeField] private string itemName;
    [SerializeField] private string displayName;
    [SerializeField] private int currentValue;
    [SerializeField] private float currentChance;
    [SerializeField] private Sprite currentSprite;

    public int Quantity {
        get => currentValue;
        set {
            currentValue = value;
            PlayerPrefs.SetInt(itemName, currentValue);
        }
    }

    public string ItemName {
        get => itemName;
    }

    public string ItemDisplayName {
        get => displayName;
    }

    public float ItemChance {
        get => currentChance;
    }

    public void SetupStorageItem(int value, string name, float chance, string customName = null, int index = 0)
    {
        indexText.text = (index + 1).ToString();
        currentValue = value;
        itemName = name;
        displayName = !string.IsNullOrEmpty(customName) ? customName : name;
        currentChance = chance;

        valueInputField.text = currentValue.ToString();
        nameInputField.text = displayName;
        chanceInputField.text = currentChance.ToString();
    }

    public void ChangeValue()
    {
        if (int.TryParse(valueInputField.text, out int newValue))
        {
            currentValue = Mathf.Max(0, newValue); // Ensure value is not negative
            valueInputField.text = currentValue.ToString();
            PlayerPrefs.SetInt(itemName, currentValue);
        }
    }

    public void UpdateChance()
    {
        if (float.TryParse(chanceInputField.text, out float newChance))
        {
            currentChance = Mathf.Clamp(newChance, 0f, 100f); // Ensure chance is between 0 and 100
            chanceInputField.text = currentChance.ToString("F2");
            PlayerPrefs.SetFloat(itemName + "_chance", currentChance);
        }

        // Update the display name if it was changed
        string newName = nameInputField.text.Trim();
        if (!string.IsNullOrEmpty(newName) && newName != displayName)
        {
            displayName = newName;
        }
    }

    public void SubtractValue()
    {
        if (currentValue > 0)
        {
            currentValue--;
            valueInputField.text = currentValue.ToString();
            PlayerPrefs.SetInt(itemName, currentValue);
        }
    }

    public void SetChance(float chance)
    {
        currentChance = chance;
        chanceInputField.text = chance.ToString("F2");
    }

    public void SetDisplayName(string newName)
    {
        displayName = newName;
        ChangeValue();
    }

    public int GetCurrentValue()
    {
        return currentValue;
    }

    public float GetCurrentChance()
    {
        return currentChance;
    }

    public string GetCurrentName()
    {
        return itemName;
    }

    public string GetCurrentDisplayName()
    {
        return displayName;
    }

    public Sprite GetCurrentSprite()
    {
        return currentSprite;
    }
}
