using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RDStationManager : MonoBehaviour
{
    [Header("RDStation Configuration")]
    [SerializeField] private string apiKey = ""; // Sua API Key da RD Station
    [SerializeField] private string rdStationBaseUrl = "https://api.rd.services"; // ou https://api.rdstation.com

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    // Endpoint correto para conversões
    private const string CONVERSION_ENDPOINT = "/platform/conversions";

    [System.Serializable]
    public class ConversionData
    {
        public string conversion_identifier;
        public string email;
        public string name;
        public string city;
        public string mobile_phone;
        public string job_title;
        public string cf_renda_7_tiers;
        public string cf_estado_civil;
    }

    public static RDStationManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Envia dados de conversão para RD Station usando JObject
    /// </summary>
    public void SendConversion(JObject conversionData, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            LogError("API Key não configurada. Configure sua API Key da RD Station.");
            onError?.Invoke("API Key não configurada");
            return;
        }

        // Garantir que conversion_identifier está definido
        if (conversionData["conversion_identifier"] == null)
            conversionData["conversion_identifier"] = "unity_game_conversion";

        // Validar dados obrigatórios
        if (!ValidateConversionData(conversionData))
        {
            onError?.Invoke("Dados de conversão inválidos");
            return;
        }

        StartCoroutine(SendConversionCoroutine(conversionData, onSuccess, onError));
    }

    /// <summary>
    /// Envia dados de conversão usando ConversionData object
    /// </summary>
    public void SendConversion(ConversionData conversionData, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        string jsonString = JsonConvert.SerializeObject(conversionData, Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        JObject jObject = JObject.Parse(jsonString);
        SendConversion(jObject, onSuccess, onError);
    }

    /// <summary>
    /// Cria objeto ConversionData com parâmetros específicos
    /// </summary>
    public ConversionData CreateConversionData(string conversionIdentifier, string email, string name,
                                             string city = null, string mobilePhone = null, string jobTitle = null,
                                             string rendaTiers = null, string estadoCivil = null)
    {
        return new ConversionData
        {
            conversion_identifier = conversionIdentifier,
            email = email,
            name = name,
            city = city,
            mobile_phone = mobilePhone,
            job_title = jobTitle,
            cf_renda_7_tiers = rendaTiers,
            cf_estado_civil = estadoCivil
        };
    }

    private IEnumerator SendConversionCoroutine(JObject conversionData, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{rdStationBaseUrl}{CONVERSION_ENDPOINT}?api_key={apiKey}";

        // Criar payload no formato da API RD Station
        JObject payload = new JObject();

        // Campos obrigatórios
        payload["conversion_identifier"] = conversionData["conversion_identifier"]?.ToString() ?? "unity_game_conversion";

        if (conversionData["email"] != null)
            payload["email"] = conversionData["email"].ToString();

        // Campos opcionais padrão
        AddFieldIfExists(payload, conversionData, "name");

        // Mapear diferentes nomes de campos
        AddMappedField(payload, conversionData, "city", new[] { "cidade", "city" });
        AddMappedField(payload, conversionData, "mobile_phone", new[] { "telefone", "mobile_phone", "phone" });
        AddMappedField(payload, conversionData, "job_title", new[] { "profissao", "job_title", "profession" });

        // Campos customizados
        AddMappedField(payload, conversionData, "cf_estado_civil", new[] { "custom1", "cf_estado_civil", "estado_civil" });
        AddMappedField(payload, conversionData, "cf_renda_7_tiers", new[] { "custom2", "cf_renda_7_tiers", "renda" });

        // Remover campos nulos/vazios
        RemoveNullOrEmptyFields(payload);

        // CORREÇÃO: Criar o objeto principal com a estrutura correta da API RD Station
        JObject requestBody = new JObject
        {
            ["event_type"] = "CONVERSION",
            ["event_family"] = "CDP",
            ["payload"] = payload
        };

        string jsonPayload = requestBody.ToString(Formatting.None);
        LogDebug($"Enviando conversão para RD Station: {jsonPayload}");

        // Criar requisição
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers corretos
            request.SetRequestHeader("Content-Type", "application/json");
            //request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Accept", "application/json");

            // Timeout
            request.timeout = 30;

            yield return request.SendWebRequest();

            // Tratar resposta
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug($"Conversão enviada com sucesso! Response: {request.downloadHandler.text}");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"Falha ao enviar conversão. Status: {request.responseCode}, Erro: {request.error}";

                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    errorMessage += $", Response: {request.downloadHandler.text}";
                }

                LogError(errorMessage);
                LogError($"URL da requisição: {url}");
                LogError($"Payload enviado: {jsonPayload}");
                LogError($"API Key configurada: {!string.IsNullOrEmpty(apiKey)}");

                // Verificar headers de resposta para debug
                var responseHeaders = request.GetResponseHeaders();
                if (responseHeaders != null)
                {
                    LogError($"Response Headers: {string.Join(", ", responseHeaders)}");
                }

                onError?.Invoke(errorMessage);
            }
        }
    }

    /// <summary>
    /// Adiciona campo se existir no objeto de origem
    /// </summary>
    private void AddFieldIfExists(JObject target, JObject source, string fieldName)
    {
        if (source[fieldName] != null && !string.IsNullOrEmpty(source[fieldName].ToString()))
        {
            target[fieldName] = source[fieldName];
        }
    }

    /// <summary>
    /// Adiciona campo mapeado de múltiplas possibilidades
    /// </summary>
    private void AddMappedField(JObject target, JObject source, string targetField, string[] possibleSourceFields)
    {
        foreach (string sourceField in possibleSourceFields)
        {
            if (source[sourceField] != null && !string.IsNullOrEmpty(source[sourceField].ToString()))
            {
                target[targetField] = source[sourceField];
                return;
            }
        }
    }

    /// <summary>
    /// Remove campos nulos ou vazios do payload
    /// </summary>
    private void RemoveNullOrEmptyFields(JObject obj)
    {
        var propsToRemove = new System.Collections.Generic.List<string>();

        foreach (var prop in obj)
        {
            if (prop.Value == null || string.IsNullOrEmpty(prop.Value.ToString()))
            {
                propsToRemove.Add(prop.Key);
            }
        }

        foreach (string prop in propsToRemove)
        {
            obj.Remove(prop);
        }
    }

    /// <summary>
    /// Valida dados de conversão antes do envio
    /// </summary>
    public bool ValidateConversionData(JObject conversionData)
    {
        if (conversionData == null)
        {
            LogError("Dados de conversão são nulos");
            return false;
        }

        // Verificar campos obrigatórios
        string conversionId = conversionData["conversion_identifier"]?.ToString();
        if (string.IsNullOrEmpty(conversionId))
        {
            LogError("conversion_identifier é obrigatório");
            return false;
        }

        string email = conversionData["email"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            LogError("email é obrigatório");
            return false;
        }

        // Validar formato do email
        if (!IsValidEmail(email))
        {
            LogError($"Formato de email inválido: {email}");
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Testa conectividade com RD Station
    /// </summary>
    public void TestConnection(System.Action<bool> callback)
    {
        StartCoroutine(TestConnectionCoroutine(callback));
    }

    private IEnumerator TestConnectionCoroutine(System.Action<bool> callback)
    {
        // Teste simples enviando dados mínimos
        JObject testData = new JObject
        {
            ["conversion_identifier"] = "test_connection",
            ["email"] = "test@example.com"
        };

        bool success = false;
        SendConversion(testData,
            onSuccess: () => { success = true; },
            onError: (error) => {
                LogError($"Teste de conexão falhou: {error}");
                success = false;
            });

        // Aguardar resultado
        yield return new WaitForSeconds(5f);
        callback?.Invoke(success);
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[RDStationManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[RDStationManager] {message}");
    }

    #region Métodos de Exemplo

    /// <summary>
    /// Exemplo de uso simples
    /// </summary>
    public void ExampleUsageSimple()
    {
        var conversionData = CreateConversionData(
            conversionIdentifier: "memory_game_completion",
            email: "jogador@exemplo.com",
            name: "João Silva",
            city: "Criciúma",
            mobilePhone: "+5548999999999"
        );

        SendConversion(conversionData,
            onSuccess: () => {
                Debug.Log("Conversão enviada com sucesso!");
            },
            onError: (error) => {
                Debug.LogError($"Erro ao enviar conversão: {error}");
            });
    }

    /// <summary>
    /// Exemplo com JObject e mapeamento personalizado
    /// </summary>
    public void ExampleUsageAdvanced()
    {
        JObject conversionData = new JObject
        {
            ["conversion_identifier"] = "unity_game_score",
            ["email"] = "player@example.com",
            ["name"] = "Maria Santos",
            ["cidade"] = "Criciúma",           // Será mapeado para "city"
            ["telefone"] = "+5548999888777",   // Será mapeado para "mobile_phone"
            ["profissao"] = "Designer",        // Será mapeado para "job_title"
            ["custom1"] = "Solteiro(a)",       // Será mapeado para "cf_estado_civil"
            ["custom2"] = "R$5.000 - R$10.000" // Será mapeado para "cf_renda_7_tiers"
        };

        if (ValidateConversionData(conversionData))
        {
            SendConversion(conversionData,
                onSuccess: () => {
                    LogDebug("Conversão avançada enviada!");
                },
                onError: (error) => {
                    LogError($"Falha na conversão avançada: {error}");
                });
        }
    }

    #endregion
}