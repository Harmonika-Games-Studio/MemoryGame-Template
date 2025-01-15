using Harmonika.MenuManager;
using UnityEngine;

public class CustomWebAutoLeadForm : LeadCaptation
{
    /*[HideInInspector]*/ public LeadDataConfig[] leadDataConfig;
    
    [Header("References")]
    [SerializeField] private Transform viewportContent;
    [SerializeField] private GameObject leadboxPrefab;

    protected override void Start() { }

    public void InstantiateLeadboxes()
    {
        base.Start();

        foreach (var config in leadDataConfig)
        {
            LeadFieldBox leadFieldBox = Instantiate(leadboxPrefab, viewportContent).GetComponent<LeadFieldBox>();
            leadFieldBox.ApplyConfig(config);
            _formInputs.Add(new FormInput(leadFieldBox.gameObject.name, leadFieldBox.InputType, leadFieldBox.GetInputObject(), leadFieldBox.IsOptional));
            leadFieldBox.onValueChanged += (string a) =>
            {
                bool aux = CheckInputsFilled();
                Debug.Log("CheckInputsFilled: " + aux);
                submitButton.gameObject.SetActive(aux);
            };
        }
    }
}
