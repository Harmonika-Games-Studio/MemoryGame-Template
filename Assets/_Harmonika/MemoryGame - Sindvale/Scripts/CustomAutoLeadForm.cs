using NaughtyAttributes;
using UnityEngine;

namespace Harmonika.Tools
{
    public class CustomAutoLeadForm : LeadCaptation
    {
        public LeadFieldBox authorizizationLeadBox;
        public CustomLeadDataConfig[] leadDataConfig;

        [Header("References")]
        [SerializeField] private Transform viewportContent;
        [SerializeField] private GameObject leadboxPrefab;

        protected override void Start()
        {
            if (authorizizationLeadBox == null)
                Debug.LogWarning("There is no Authorization toggle assigned on CustomAutoLeadForm.cs!");

            InstantiateLeadboxes();
        }

        public void InstantiateLeadboxes()
        {
            if (leadDataConfig == null)
                return;

            base.Start();

            _formInputs.Add(new FormInput(authorizizationLeadBox.FieldName, authorizizationLeadBox.InputType, authorizizationLeadBox.GetInputObject(), authorizizationLeadBox.ID, authorizizationLeadBox.IsOptional));
            authorizizationLeadBox.onValueChanged += (string a) =>
            {
                bool aux = CheckInputsFilled();
                Debug.Log("CheckInputsFilled: " + aux);
                submitButton.gameObject.SetActive(aux);
            };

            foreach (var config in leadDataConfig)
            {
                CustomLeadFieldBox leadFieldBox = Instantiate(leadboxPrefab, viewportContent).GetComponent<CustomLeadFieldBox>();
                leadFieldBox.ApplyConfig(config);
                _formInputs.Add(new FormInput(leadFieldBox.FieldName, leadFieldBox.InputType, leadFieldBox.GetInputObject(), leadFieldBox.ID, leadFieldBox.IsOptional));
                leadFieldBox.onValueChanged += (string a) =>
                {
                    bool aux = CheckInputsFilled();
                    Debug.Log("CheckInputsFilled: " + aux);
                    submitButton.gameObject.SetActive(aux);
                };

                leadFieldBox.OnTypeValueChanged();
            }
        }
    }

    #region CustomLeadDataConfig
    public enum CustomParseableFields
    {
        none,
        cpf,
        cnpj,
        cpf_and_cnpj,
        phone,
        cep,
        date
    }

    [System.Serializable]
    public class CustomLeadDataConfig
    {
        [Header("Config")]
        public string fieldName;
        public LeadID id;
        public bool isOptional;

        [AllowNesting]
        [OnValueChanged(nameof(OnTypeValueChanged))]
        public LeadInputType inputType;

        private bool _inputField, _dropdown, _toggle;

        [AllowNesting]
        [ShowIf(nameof(_toggle))]
        public ToggleDataConfig toggleDataConfig;

        [AllowNesting]
        [ShowIf(nameof(_inputField))]
        public CustomInputDataConfig inputDataConfig;

        [AllowNesting]
        [ShowIf(nameof(_dropdown))]
        public DropdownDataConfig dropdownDataConfig;

        void OnTypeValueChanged()
        {
            switch (inputType)
            {
                case LeadInputType.InputField:
                    _inputField = true;
                    _dropdown = false;
                    _toggle = false;
                    break;
                case LeadInputType.Dropdown:
                    _inputField = false;
                    _dropdown = true;
                    _toggle = false;
                    break;
                case LeadInputType.Toggle:
                    _inputField = false;
                    _dropdown = false;
                    _toggle = true;
                    break;
            }
        }

        private void FakeUsage() //Fake method to avoid 3 annoying warnings in the console
        {
            if (_inputField) { }
            if (_dropdown) { }
            if (_toggle) { }
        }
    }

    [System.Serializable]
    public class CustomInputDataConfig
    {
        public KeyboardType keyboardType;
        public CustomParseableFields customParseableFields;
        public string fieldPlaceholder;

        public CustomInputDataConfig(KeyboardType keyboardType, CustomParseableFields parseableFields, string fieldPlaceholder)
        {
            this.keyboardType = keyboardType;
            this.customParseableFields = parseableFields;
            this.fieldPlaceholder = fieldPlaceholder;
        }

        public CustomInputDataConfig(string keyboardType, string customParseableFields, string fieldPlaceholder)
        {
            if (System.Enum.TryParse(keyboardType, out KeyboardType parsedKeyboardType))
                this.keyboardType = parsedKeyboardType;
            else
                throw new System.ArgumentException($"Invalid KeyboardType: {keyboardType}");

            if (System.Enum.TryParse(customParseableFields, out CustomParseableFields customParsedParseableFields))
                this.customParseableFields = customParsedParseableFields;
            else
                throw new System.ArgumentException($"Invalid ParseableFields: {customParseableFields}");

            this.fieldPlaceholder = fieldPlaceholder;
        }
    }
    #endregion
}