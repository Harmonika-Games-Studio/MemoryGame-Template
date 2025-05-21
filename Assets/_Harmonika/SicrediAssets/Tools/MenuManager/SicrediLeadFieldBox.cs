using UnityEngine;
using NaughtyAttributes;

public class SicrediLeadFieldBox : MonoBehaviour {

    [OnValueChanged(nameof(OnTypeValueChanged))]
    [SerializeField] private LeadInputType _type;
    [SerializeField] private GameObject _input_go, _toggle_go, _dropdown_go, _searchDropdown_go;

    public LeadInputType Type {
        get {
            return _type;
        }
        set {
            _type = value;

            OnTypeValueChanged();
        }
    }

    private void OnTypeValueChanged() {
        switch (_type) {
            case LeadInputType.InputField:
                Debug.Log("Type changed to InputField");
                _input_go.SetActive(true);
                _dropdown_go.SetActive(false);
                _toggle_go.SetActive(false);
                _searchDropdown_go.SetActive(false);
                break;
            case LeadInputType.Toggle:
                Debug.Log("Type changed to Toggle");
                _input_go.SetActive(false);
                _dropdown_go.SetActive(false);
                _toggle_go.SetActive(true);
                _searchDropdown_go.SetActive(false);
                break;
            case LeadInputType.Dropdown:
                Debug.Log("Type changed to Dropdown");
                _input_go.SetActive(false);
                _dropdown_go.SetActive(true);
                _toggle_go.SetActive(false);
                _searchDropdown_go.SetActive(false);
                break;
            case LeadInputType.SearchableDropdown:
                Debug.Log("Type changed to Dropdown");
                _input_go.SetActive(false);
                _dropdown_go.SetActive(false);
                _toggle_go.SetActive(false);
                _searchDropdown_go.SetActive(true);
                break;
        }
    }
}

