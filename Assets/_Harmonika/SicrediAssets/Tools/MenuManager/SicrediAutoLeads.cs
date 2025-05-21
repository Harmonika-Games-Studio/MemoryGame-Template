using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NaughtyAttributes;
using Harmonika.Tools;

[System.Serializable]
public class LeadDataConfig
{
    [Header("Config")]
    public string fieldName;
    public LeadInputType inputType;
    public KeyboardType keyboardType;
    public bool isOptional;
}

public enum LeadInputType
{
    InputField,
    Dropdown,
    Toggle,
    SearchableDropdown
}

public class SicrediAutoLeads : MonoBehaviour
{
    [Header("Automatic Data Fields")]
    public LeadDataConfig[] leadDataConfig;

    [Header("References")]
    [SerializeField] private Transform viewportContent;
    [SerializeField] private GameObject leadboxPrefab;

    [Header("Button")]
    public Button submitButton;

    [Header("UI Elements")]
    public List<SicrediFormInput> formInputs;
    public Action<JObject> OnSubmitEvent;

    void Start()
    {
        submitButton.gameObject.SetActive(false);
        SetupForm();
    }

    private void InstantiateLeadboxes()
    {
        foreach (var item in leadDataConfig)
        {
            GameObject obj = Instantiate(leadboxPrefab, viewportContent);
        }
    }

    private void SetupForm()
    {
        foreach (var input in formInputs)
        {
            switch (input.inputType)
            {
                case LeadInputType.InputField:
                    TMP_InputField inputField = input.inputContainer.GetComponent<TMP_InputField>();
                    if (inputField != null)
                    {
                        inputField.onValueChanged.AddListener(delegate { submitButton.gameObject.SetActive(CheckInputsFilled()); });
                    }
                    break;

                case LeadInputType.Dropdown:
                    TMP_Dropdown dropdown = input.inputContainer.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        dropdown.onValueChanged.AddListener(delegate { submitButton.gameObject.SetActive(CheckInputsFilled()); });
                    }
                    break;

                case LeadInputType.Toggle:
                    Toggle toggle = input.inputContainer.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.onValueChanged.AddListener(delegate { submitButton.gameObject.SetActive(CheckInputsFilled()); });
                    }
                    break;

                case LeadInputType.SearchableDropdown:
                    SicrediSearchableDropdown searchableDropdown = input.inputContainer.GetComponent<SicrediSearchableDropdown>();
                    if (searchableDropdown != null)
                    {
                        searchableDropdown.onValueChanged.AddListener(delegate { submitButton.gameObject.SetActive(CheckInputsFilled()); });
                    }
                    break;
            }
        }

        submitButton.onClick.AddListener(SubmitForm);
    }

    private void SubmitForm()
    {
        JObject jsonData = GrabAllData();
        Debug.Log("Here's your data, bro! " + jsonData.ToString());

        OnSubmitEvent?.Invoke(jsonData);
    }

    JObject GrabAllData()
    {
        JObject jsonObject = new JObject(); 

        foreach (var input in formInputs)
        {
            string inputData = string.Empty;

            switch (input.inputType)
            {
                case LeadInputType.InputField:
                    TMP_InputField inputField = input.inputContainer.GetComponent<TMP_InputField>();
                    if (inputField != null)
                    {
                        inputData = InputFieldToString(inputField);
                    }
                    break;

                case LeadInputType.Dropdown:
                    TMP_Dropdown dropdown = input.inputContainer.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        inputData = DropdownToString(dropdown);
                    }
                    break;

                case LeadInputType.Toggle:
                    Toggle toggle = input.inputContainer.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        inputData = ToggleToString(toggle.isOn);
                    }
                    break;

                case LeadInputType.SearchableDropdown:
                    SicrediSearchableDropdown searchableDropdown = input.inputContainer.GetComponent<SicrediSearchableDropdown>();
                    if (searchableDropdown != null)
                    {
                        inputData = DropdownToString(searchableDropdown);
                    }
                    break;
            }

            jsonObject.Add(input.inputName, inputData);
        }

        return jsonObject;
    }

    private string ToggleToString(bool toggle)
    {
        return toggle ? "Sim" : "Não";
    }

    private string DropdownToString(TMP_Dropdown dropdown)
    {
        return dropdown.options[dropdown.value].text;
    }

    private string InputFieldToString(TMP_InputField inputField)
    {
        return inputField.text;
    }

    public bool CheckInputsFilled()
    {
        foreach (var input in formInputs)
        {
            if (input.isOptional)
                continue;

            switch (input.inputType)
            {
                case LeadInputType.InputField:
                    TMP_InputField inputField = input.inputContainer.GetComponent<TMP_InputField>();
                    if (string.IsNullOrEmpty(inputField.text))
                        return false;
                    break;

                case LeadInputType.Dropdown:
                    TMP_Dropdown dropdown = input.inputContainer.GetComponent<TMP_Dropdown>();
                    if (dropdown.value == -1)
                        return false;
                    break;

                case LeadInputType.Toggle:
                    Toggle toggle = input.inputContainer.GetComponent<Toggle>();
                    if (!toggle.isOn)
                        return false;
                    break;
                
                case LeadInputType.SearchableDropdown:
                    SicrediSearchableDropdown searchableDropdown = input.inputContainer.GetComponent<SicrediSearchableDropdown>();
                    if (searchableDropdown.value == -1)
                        return false;
                    break;
            }
        }

        return true;
    }
}