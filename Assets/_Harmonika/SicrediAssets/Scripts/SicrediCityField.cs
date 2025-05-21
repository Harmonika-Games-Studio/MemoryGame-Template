using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public class CooperativeCity
{
    public string Cidade;
    public string Cooperativa;
}

public class SicrediCityField : MonoBehaviour
{
    [SerializeField] private TextAsset _cityStateJsonFile;

    [SerializeField] SearchableDropdown _cityDropdown;
    [SerializeField] TMP_InputField _cityInputField;
    [SerializeField] TMP_Dropdown _stateDropdown;

    private UnityAction<int> unityAction;

    private List<Cidade> _cityList = new List<Cidade>();
    private List<Estado> _stateList = new List<Estado>();
    private List<string> _filteredCities = new List<string>();
    private List<CityStateData> _cityStateDataList = new List<CityStateData>();

    void Start()
    {
        //StartCoroutine(FetchStateCityData());
        //_cityInputField.onValueChanged.AddListener(OnCityInputChanged);
    }

    private void PopulateStateDropdown()
    {
        _stateDropdown.options.Clear();

        foreach (CityStateData data in _cityStateDataList)
        {
            _stateDropdown.options.Add(new TMP_Dropdown.OptionData(data.siglaEstado));
        }

        _stateDropdown.onValueChanged.AddListener(OnStateSelected);

        _cityDropdown.interactable = false;
        _cityInputField.interactable = false;
    }

    private void OnStateSelected(int index)
    {
        // Limpa o dropdown de cidades e habilita as interações
        _cityDropdown.options.Clear();
        _cityDropdown.interactable = true;
        _cityInputField.interactable = true;

        // Obtém a sigla do estado selecionado e remove espaços extras
        string selectedState = _cityStateDataList[index].siglaEstado.Trim();
        Debug.Log($"Estado selecionado: '{selectedState}'");

        _filteredCities = _cityStateDataList
            .FirstOrDefault(data => data.siglaEstado.Trim().Equals(selectedState, StringComparison.OrdinalIgnoreCase))
            ?.nomeCidades
            .OrderBy(city => city)
            .ToList() ?? new List<string>();


        // Adiciona as cidades filtradas ao dropdown
        foreach (var city in _filteredCities)
        {
            _cityDropdown.options.Add(new TMP_Dropdown.OptionData(city));
        }

        // Debug para validar as cidades filtradas
        Debug.Log($"Cidades filtradas para o estado {selectedState}: {string.Join(", ", _filteredCities.Select(c => c))}");

        // Restaura o estado inicial do dropdown
        _cityDropdown.value = -1;
        _cityDropdown.UpdateOriginalOptions();
    }


    private void OnCityInputChanged(string input)
    {
        _cityDropdown.options.Clear();

        // Filtra cidades, garantindo que os dados são válidos
        var matchingCities = _filteredCities
            .Where(city => city != null && !string.IsNullOrEmpty(city) &&
                           city.Contains(input, StringComparison.OrdinalIgnoreCase)) //Ignorar assentos, espaços e pontuações como hífens
            .ToList();

        foreach (var city in matchingCities)
        {
            _cityDropdown.options.Add(new TMP_Dropdown.OptionData(city));
        }

        //_cityDropdown.value = -1;
        _cityDropdown.UpdateOriginalOptions();

        // Debug para verificar cidades filtradas
        Debug.Log($"Cidades correspondentes para a entrada '{input}': {string.Join(", ", matchingCities.Select(c => c))}");
    }

    private IEnumerator FetchStateCityData()
    {
        // Tenta carregar os dados do arquivo
        if (LoadDataFromFile())
        {
            PopulateStateDropdown();
            yield break; // Não precisa baixar os dados
        }

        // Se não conseguiu carregar, baixa da API
        LoadingScript.Instance.Loading = true;
        string url = "https://brasilapi.com.br/api/ibge/uf/v1";
        bool success = false;

        while (!success)
        {
            UnityWebRequest requestStates = UnityWebRequest.Get(url);
            yield return requestStates.SendWebRequest();

            if (requestStates.result == UnityWebRequest.Result.Success)
            {
                success = true;
                string jsonStates = requestStates.downloadHandler.text;

                // Deserializa e organiza os estados em ordem alfabética
                _stateList = JsonConvert.DeserializeObject<List<Estado>>(jsonStates)
                            .OrderBy(state => state.nome)
                            .ToList();

                foreach (var state in _stateList)
                {
                    yield return StartCoroutine(FetchCities(state.sigla));
                }

                SaveDataToFile();
                PopulateStateDropdown();
            }
            else
            {
                //LoadingScript.Instance.ActivateErrorMessage();
                Debug.LogError($"Error fetching states: {requestStates.error}");
                yield return new WaitForSeconds(1);
            }
        }

        LoadingScript.Instance.Loading = false;
    }

    private IEnumerator FetchCities(string stateAbbreviation)
    {
        string url = $"https://brasilapi.com.br/api/ibge/municipios/v1/{stateAbbreviation.ToLower()}";
        bool success = false;

        while (!success)
        {
            UnityWebRequest requestCities = UnityWebRequest.Get(url);
            yield return requestCities.SendWebRequest();

            if (requestCities.result == UnityWebRequest.Result.Success)
            {
                CityStateData _cityStateData = new();
                _cityStateData.siglaEstado = stateAbbreviation;
                success = true;
                string jsonCities = requestCities.downloadHandler.text;
                var cities = JsonConvert.DeserializeObject<List<Cidade>>(jsonCities)
                      .Where(city => city != null && !string.IsNullOrEmpty(city.nome))
                      .Select(city => city.nome)
                      .OrderBy(cityName => cityName)
                      .ToList();

                _cityStateData.nomeCidades = cities; //tenho uma lista de CityStateData, cada membor dessa lsita representa um estado, preciso adicionar as cidades apenas
                _cityStateDataList.Add(_cityStateData);
            }
            else
            {
                //LoadingScript.Instance.ActivateErrorMessage();
                Debug.LogError($"Erro ao buscar cidades para o estado {stateAbbreviation}: {requestCities.error}");
                yield return new WaitForSeconds(1); // Aguarde antes de tentar novamente
            }
        }

    }

    private void SaveDataToFile()
    {
        // Cria o objeto que será serializado
        string json = JsonConvert.SerializeObject(_cityStateDataList, Formatting.Indented);

        // Caminho do arquivo
        string filePath = Application.persistentDataPath + "/stateCityData.json";

        // Salva o JSON no caminho especificado
        File.WriteAllText(filePath, json);

        Debug.Log($"Dados salvos no arquivo: {filePath}");
    }

    private bool LoadDataFromFile()
    {
        if(_cityStateJsonFile)
        {
            Debug.Log("Dados carregados do arquivo JSON pré configurado.");
            _cityStateDataList = JsonConvert.DeserializeObject<List<CityStateData>>(_cityStateJsonFile.text);
            return true;
        }

        // Caminho do arquivo
        string filePath = Application.persistentDataPath + "/stateCityData.json";

        // Verifica se o arquivo existe
        if (File.Exists(filePath))
        {
            // Lê e desserializa o arquivo JSON
            string json = File.ReadAllText(filePath);
            _cityStateDataList = JsonConvert.DeserializeObject<List<CityStateData>>(json);

            Debug.Log("Dados carregados do arquivo.");
            return true; // Dados carregados com sucesso
        }
        else
        {
            Debug.Log("Arquivo de dados não encontrado. Dados serão baixados.");
            return false; // Arquivo não encontrado
        }
    }

    [System.Serializable]
    private class CityStateData
    {
        public string siglaEstado;
        public List<string> nomeCidades;
    }
    
    [System.Serializable]
    private class Estado
    {
        public string sigla;
        public string nome;
    }

    [System.Serializable]
    private class Cidade
    {
        public string nome;
    }
}