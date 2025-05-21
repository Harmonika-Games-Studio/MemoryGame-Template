using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

//Atention: To this code work properly, a change is necessary on TMP_Dropdown.cs, on line 443!
//The line  m_Value = Mathf.Clamp(value, m_Placeholder ? -1 : 0, options.Count - 1); need to be changed to:
// •  "m_Value = Mathf.Clamp(value, -1, options.Count - 1);"
//Please, make this change if you want to use Searchable Dropdowns on your project.
public class SicrediSearchableDropdown : TMP_Dropdown {
    private TMP_InputField inputField;
    private List<string> originalOptions = new List<string>();
    private Coroutine searchCoroutine;
    private float _delay = 0f;

    protected override void Awake() {
        inputField = GetComponentInChildren<TMP_InputField>();
        UpdateOriginalOptions();
    }

    public void UpdateOriginalOptions() {
        originalOptions.Clear();
        foreach (var option in options) {
            originalOptions.Add(option.text);
        }
    }

    protected override void Start() {
        base.Start();
        inputField.onSubmit.AddListener(OnSearchInputChanged);
        inputField.onSelect.AddListener((string a) => { inputField.text = string.Empty; });
        onValueChanged.AddListener(OnValueChanged);
    }

    private void OnSearchInputChanged(string searchText) {

        if (string.IsNullOrEmpty(inputField.text)) return;

        if (searchCoroutine != null) {
            StopCoroutine(searchCoroutine);
        }

        searchCoroutine = StartCoroutine(DelayedFilterDropdown(searchText));
    }

    private void OnValueChanged(int i) {
        Debug.Log($"Dropdown Value: {i}");
        if (i == -1) return;

        Debug.Log($"Option selected: {options[i].text}");

        inputField.onSubmit.RemoveAllListeners();

        inputField.text = options[i].text;

        inputField.onSubmit.AddListener(OnSearchInputChanged);

        Invoke(nameof(Deselect), 0.2f);
    }

    void Deselect() {
        if (onValueChanged != null) {
            onValueChanged.RemoveListener(OnValueChanged);
        }

        value = -1;

        if (onValueChanged != null) {
            onValueChanged.AddListener(OnValueChanged);
        }
    }

    private IEnumerator DelayedFilterDropdown(string searchText) {
        yield return new WaitForSeconds(_delay);
        UpdateDropdownOptions(searchText);
    }

    private void UpdateDropdownOptions(string filter) {
        var filteredOptions = originalOptions
            .Where(option => !string.IsNullOrEmpty(option) && option.ToLower().Contains(filter.ToLower()))
            .OrderBy(option => option)
            .ToList();

        options.Clear();
        foreach (var option in filteredOptions) {
            options.Add(new OptionData(option));
            Debug.Log($"Opção adicionada ao dropdown: {option}");
        }

        RefreshShownValue();
        Show();

        Debug.Log($"Dropdown atualizado com {options.Count} opções visíveis.");
    }
}

