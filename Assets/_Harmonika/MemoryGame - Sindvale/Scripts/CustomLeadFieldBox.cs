using Harmonika.Tools;
using Harmonika.Tools.Keyboard;
using NaughtyAttributes;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CustomLeadFieldBox : MonoBehaviour
{
    [OnValueChanged(nameof(OnTypeValueChanged))]
    [SerializeField] private LeadInputType _type;
    [SerializeField] private LeadID _id;
    [SerializeField] private TMP_Text _fieldName;

    [SerializeField] private TMP_InputField _input_field;
    [SerializeField] private TMP_Dropdown _dropdown;
    [SerializeField] private ToggleWithText _toggle;
    [SerializeField] private bool _isOptional;

    private bool _configuredByCode = false;

    public UnityAction<string> onValueChanged = delegate { };

    #region Properties
    public string FieldName { get => _fieldName.text; }
    public LeadInputType InputType
    {
        get => _type;
        set
        {
            _type = value;
            OnTypeValueChanged();
        }
    }
    public bool IsOptional { get => _isOptional; }
    public LeadID ID { get => _id; }
    #endregion

    private void Awake()
    {
        Invoke(nameof(Setup), .01f);
    }

    void Setup()
    {
        if (_configuredByCode)
            return;

        switch (InputType)
        {
            case LeadInputType.InputField:
                _input_field.onValueChanged.AddListener((string a) => onValueChanged.Invoke(a));
                break;

            case LeadInputType.Dropdown:
                _dropdown.onValueChanged.AddListener((int a) => onValueChanged.Invoke(a.ToString()));
                break;

            case LeadInputType.Toggle:
                _toggle.onValueChanged.AddListener((bool a) => onValueChanged.Invoke(a.ToString()));
                break;
        }
    }

    private string LimitCharacters(string value, int maxLength)
    {
        if (value.Length > maxLength)
        {
            value = value.Substring(0, maxLength);
        }
        return value;
    }

    public GameObject GetInputObject()
    {
        switch (InputType)
        {
            case LeadInputType.InputField:
                return _input_field.gameObject;
            case LeadInputType.Dropdown:
                return _dropdown.gameObject;
            case LeadInputType.Toggle:
                return _toggle.gameObject;
            default:
                return null;
        }
    }

    public void ApplyConfig(CustomLeadDataConfig config)
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
                keyboardView = _input_field.gameObject.AddComponent<KeyboardView>();
                keyboardView.selectedType = config.inputDataConfig.keyboardType;
                _input_field.placeholder.GetComponent<TextMeshProUGUI>().text = config.inputDataConfig.fieldPlaceholder;
                _input_field.onSubmit.AddListener((string a) => _input_field.text = Format(a, config.inputDataConfig.customParseableFields));
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

    public void OnTypeValueChanged()
    {
        switch (_type)
        {
            case LeadInputType.InputField:
                _input_field.gameObject.SetActive(true);
                _dropdown.gameObject.SetActive(false);
                _toggle.gameObject.SetActive(false);
                break;
            case LeadInputType.Dropdown:
                _input_field.gameObject.SetActive(false);
                _dropdown.gameObject.SetActive(true);
                _toggle.gameObject.SetActive(false);
                break;
            case LeadInputType.Toggle:
                _input_field.gameObject.SetActive(false);
                _dropdown.gameObject.SetActive(false);
                _toggle.gameObject.SetActive(true);
                break;
        }
    }

    public string Format(string str, CustomParseableFields parseableField)
    {
        if (parseableField != CustomParseableFields.none) str = Regex.Replace(str, @"\D", "");

        switch (parseableField)
        {
            case CustomParseableFields.cpf:
                if (str.Length < 11)
                    return str;

                else if (str.Length >= 11)
                    str = str.Substring(0, 11);
                return Regex.Replace(str, @"(\d{3})(\d{0,3})(\d{0,3})(\d{0,2})", "$1.$2.$3-$4");


            case CustomParseableFields.cnpj:
                if (str.Length < 14)
                    return str;

                if (str.Length >= 14)
                    str = str.Substring(0, 14);

                return Regex.Replace(str, @"(\d{2})(\d{0,3})(\d{0,3})(\d{0,4})(\d{0,2})", "$1.$2.$3/$4-$5");

            case CustomParseableFields.cpf_and_cnpj:
                if (str.Length == 11)
                {
                    return Regex.Replace(str, @"(\d{3})(\d{0,3})(\d{0,3})(\d{0,2})", "$1.$2.$3-$4");
                }
                else if (str.Length >= 14)
                {
                    str = str.Substring(0, 14);
                    return Regex.Replace(str, @"(\d{2})(\d{0,3})(\d{0,3})(\d{0,4})(\d{0,2})", "$1.$2.$3/$4-$5");
                }
                return str;

            case CustomParseableFields.phone:
                if (str.Length < 11)
                    return str;

                if (str.Length >= 11)
                    str = str.Substring(0, 11);

                return Regex.Replace(str, @"(\d{2})(\d{0,5})(\d{0,4})", "($1) $2-$3");

            case CustomParseableFields.cep:
                if (str.Length < 8)
                    return str;

                if (str.Length >= 8)
                    str = str.Substring(0, 8);

                return Regex.Replace(str, @"(\d{5})(\d{0,3})", "$1-$2");

            case CustomParseableFields.date:
                if (str.Length < 8)
                    return str;

                if (str.Length >= 8)
                    str = str.Substring(0, 8);

                return Regex.Replace(str, @"(\d{2})(\d{0,2})(\d{0,4})", "$1/$2/$3");

            default:
                return str;
        }
    }
}