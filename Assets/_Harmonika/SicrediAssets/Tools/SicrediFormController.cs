using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Harmonika.Tools;
using System.Text.RegularExpressions;

public class SicrediFormController : MonoBehaviour
{
    //[Header("Parent Object")]
    //public GameObject parentPanel;

    [Header("Button")]
    public Button submitButton;

    [Header("UI Elements")]
    public List<SicrediFormInput> formInputs;

    void Start()
    {
        ActivateConfirmButtonCheck();
        SetupForm();
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
                        inputField.onValueChanged.AddListener(delegate { ActivateConfirmButtonCheck(); });
                    }
                    break;

                case LeadInputType.Dropdown:
                    TMP_Dropdown dropdown = input.inputContainer.GetComponent<TMP_Dropdown>();
                    if (dropdown != null)
                    {
                        dropdown.onValueChanged.AddListener(delegate { ActivateConfirmButtonCheck(); });
                    }
                    break;

                case LeadInputType.Toggle:
                    Toggle toggle = input.inputContainer.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.onValueChanged.AddListener(delegate { ActivateConfirmButtonCheck(); });
                    }
                    break;
            }
        }

        submitButton.onClick.AddListener(SubmitForm);
    }

    private void SubmitForm()
    {
        JObject jsonData = GrabAllData();
        jsonData["cpf"] = FormatCPF(jsonData["cpf"].ToString());
        jsonData["telefone"] = FormatPhone(jsonData["telefone"].ToString());
        jsonData["dataNascimento"] = FormatBirthDate(jsonData["dataNascimento"].ToString());
        Debug.Log("Here's your data, bro! " + jsonData.ToString());

        AppManager.Instance.DataSync.AddDataToJObject(jsonData);
    }

    private string FormatCPF(string cpf)
    {
        if (cpf.Length == 11)
        {
            return Regex.Replace(cpf, @"(\d{3})(\d{3})(\d{3})(\d{2})", "$1.$2.$3-$4");
        }
        return cpf; // Retorna sem formatação se não tiver o tamanho esperado
    }

    private string FormatPhone(string phone)
    {
        if (phone.Length == 11) // Formato 9 dígitos (Brasil)
        {
            return Regex.Replace(phone, @"(\d{2})(\d{5})(\d{4})", "($1) $2-$3");
        }
        else if (phone.Length == 10) // Formato 8 dígitos
        {
            return Regex.Replace(phone, @"(\d{2})(\d{4})(\d{4})", "($1) $2-$3");
        }
        return phone; // Retorna sem formatação se não tiver o tamanho esperado
    }

    private string FormatBirthDate(string birthDate)
    {
        if (birthDate.Length == 8) // Formato esperado: DDMMYYYY
        {
            return Regex.Replace(birthDate, @"(\d{2})(\d{2})(\d{4})", "$1/$2/$3");
        }
        return birthDate; // Retorna sem formatação se não tiver o tamanho esperado
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

    private void ActivateConfirmButtonCheck()
    {
        submitButton.gameObject.SetActive(CheckInputsFilled());
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
                    if (input.inputName == "cpf" && inputField.text.Length <= 10) return false;
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
            }
        }

        return true;
    }
}

[Serializable]
public class SicrediFormInput
{
    public string inputName;
    public LeadInputType inputType;
    public GameObject inputContainer;
    public bool isOptional;
}
