using System.Collections.Generic;
using System.Text.RegularExpressions;
using Harmonika.Tools;
using Harmonika.Tools.Keyboard;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using System.IO;
using System.Linq;

public class CustomLeadFieldbox : LeadFieldBox
{
    string _permanentDataPath;


    private void Awake()
    {
        _permanentDataPath = Application.persistentDataPath + "/permanentLocalData.json";
    }

    public override void ApplyConfig(LeadDataConfig config)
    {
        _configuredByCode = true;

        _fieldName.text = config.fieldName;
        _id = config.id;
        _isOptional = config.isOptional;
        _type = config.inputType;
        KeyboardView keyboardView;

        switch (InputType)
        {
            case LeadInputType.InputField:
                TextMeshProUGUI placeholderTMP = _input_field.placeholder.GetComponent<TextMeshProUGUI>();
                keyboardView = _input_field.gameObject.AddComponent<KeyboardView>();
                keyboardView.selectedType = config.inputDataConfig.keyboardType;
                placeholderTMP.text = config.inputDataConfig.fieldPlaceholder;
                _input_field.onSubmit.AddListener((string a) =>
                {
                    _input_field.text = a.Format(config.inputDataConfig.parseableFields);


                    if (ID == LeadID.cpf)
                    {
                        a = a.Format(ParseableFields.cpf);

                        if (!a.ValidateCPF())
                        {
                            _input_field.text = string.Empty;
                            //placeholderTMP.text = "Insira um CPF valido!";
                            //InvokeUtility.Invoke(2f, () => placeholderTMP.text = config.inputDataConfig.fieldPlaceholder);
                            PopupManager.Instance.InvokeConfirmDialog("O CPF inserido é inválido! \n Por favor, tente novamente!", "OK", true, null, 0);
                            return;
                        }


                        if (IsCpfRegistered(a))
                        {
                            _input_field.text = string.Empty;
                            //placeholderTMP.text = $"Esse CPF já foi cadastrado.";
                            //InvokeUtility.Invoke(2f, () => placeholderTMP.text = config.inputDataConfig.fieldPlaceholder);
                            PopupManager.Instance.InvokeConfirmDialog("O CPF inserido já foi utilizado! \n Por favor, insira um outro CPF!", "OK", true, null, 0);
                            return;
                        }
                    }

                });


                _input_field.onValueChanged.AddListener((string a) => onValueChanged.Invoke(a));
                break;

            case LeadInputType.Dropdown:
                _dropdown.placeholder.GetComponent<TextMeshProUGUI>().text = config.dropdownDataConfig.fieldPlaceholder;
                _dropdown.onValueChanged.AddListener((int a) => onValueChanged.Invoke(a.ToString()));

                _dropdown.options.Clear();
                foreach (var option in config.dropdownDataConfig.options)
                {
                    _dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                }
                break;

            case LeadInputType.Toggle:
                _toggle.tmpText.text = config.toggleDataConfig.text;
                _toggle.onValueChanged.AddListener((bool a) => onValueChanged.Invoke(a.ToString()));
                break;
        }
    }

    private bool IsCpfRegistered(string cpf)
    {
        if (!File.Exists(_permanentDataPath))
        {
            Debug.LogError("Ainda não tem dados pra buscar.");
            return false;
        }

        string jsonContent = File.ReadAllText(_permanentDataPath);

        JObject jsonData;
        try
        {
            jsonData = JObject.Parse(jsonContent);
        }
        catch
        {
            Debug.LogError("Erro ao processar o arquivo JSON. Verifique o formato.");
            return false;
        }

        if (jsonData.ContainsKey("Data"))
        {
            var dataList = jsonData["Data"].ToObject<List<JObject>>();

            return dataList.Any(record =>
            {
                var recordCpf = record["cpf"]?.ToString();
                return recordCpf != null && recordCpf.Equals(cpf);
            });
        }

        return false;
    }
}
