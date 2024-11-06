using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CollectLeadsMenu : MonoBehaviour
{
    [SerializeField] private Button _continueGame;
    [SerializeField] private Button _back;
    [SerializeField] private TMP_Text _collectLeadsTitle;
    [SerializeField] private FormController _form;

    public string LeadsText
    {
        get => _collectLeadsTitle.text;
        set
        {
            _collectLeadsTitle.text = value;
        }
    }

    public void AddContinueGameButtonListener(UnityAction action)
    {
        _continueGame.onClick.AddListener(action);
    }
    
    public void AddBackButtonListener(UnityAction action)
    {
        _back.onClick.AddListener(action);
    }

    public void ChangeVisualIdentity(Color primary)
    {
        _collectLeadsTitle.color = primary;
    }

    public void ClearTextFields()
    {
        foreach (var input in _form.formInputs)
        {
            if(input.inputContainer.TryGetComponent(out TMP_InputField tmpText))
            {
                tmpText.text = "";
            }
        }
        
    }
}
