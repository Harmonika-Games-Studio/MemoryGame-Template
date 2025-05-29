using Harmonika.Tools;
using Harmonika.Tools.Keyboard;
using TMPro;
using UnityEngine;

public class CustomLeadFieldbox : LeadFieldBox
{
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
                        if (!a.ValidateCPF())
                        {
                            _input_field.text = string.Empty;
                            placeholderTMP.text = "Insira um CPF valido!";
                            InvokeUtility.Invoke(2f, () => placeholderTMP.text = config.inputDataConfig.fieldPlaceholder);
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
}
