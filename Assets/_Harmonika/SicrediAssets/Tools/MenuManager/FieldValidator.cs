using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class FieldValidator : MonoBehaviour
{

    [SerializeField] private TMP_InputField _input;
    [SerializeField] private string _fieldKey;
    private string permanentDataPath;
    [SerializeField] private GameObject _failValidationMessage;

    void Awake() {
        permanentDataPath = Application.persistentDataPath + "/permanentLocalData.json";
    } 

    void Start()
    {

        _input.onSubmit.AddListener((string cpf) =>
        {
            if (IsCpfRegistered(cpf))
            {
                Debug.LogWarning($"O CPF {cpf} já está cadastrado.");
                Invoke(nameof(FailValidation), .05f);
            }
            else
            {
                Debug.Log($"CPF {cpf} não está cadastrado.");
                _failValidationMessage.SetActive(false);
            }
        });
    }

    void FailValidation()
    {
        _input.placeholder.gameObject.GetComponent<TMP_Text>().text = "Insira um CPF diferente";
        _input.text = string.Empty;
        _failValidationMessage.SetActive(true);
    }

    private bool IsCpfRegistered(string cpf)
    {
        if (cpf.Length == 11)
        {
            cpf = Regex.Replace(cpf, @"(\d{3})(\d{3})(\d{3})(\d{2})", "$1.$2.$3-$4");
        }

        if (!File.Exists(permanentDataPath))
        {
            Debug.LogWarning("Arquivo permanente não encontrado. Nenhum CPF está registrado.");
            return false;
        }

        string jsonContent = File.ReadAllText(permanentDataPath);

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