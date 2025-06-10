using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    [Header("Offline Storage Settings")]
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float retryDelaySeconds = 5f;
    [SerializeField] private int maxStoredConversions = 100; // M�ximo de convers�es armazenadas offline
    [SerializeField] private float autoRetryIntervalSeconds = 30f; // Intervalo para tentar reenviar dados pendentes

    // Endpoint correto para convers�es
    private const string CONVERSION_ENDPOINT = "/platform/conversions";
    private const string OFFLINE_DATA_FILENAME = "rd_station_offline_data.json";

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

    [System.Serializable]
    public class OfflineConversionEntry
    {
        public string jsonData;
        public int retryCount;
        public long timestamp;
        public string id;

        public OfflineConversionEntry(string data)
        {
            jsonData = data;
            retryCount = 0;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            id = Guid.NewGuid().ToString();
        }
    }

    [System.Serializable]
    public class OfflineDataContainer
    {
        public List<OfflineConversionEntry> pendingConversions = new List<OfflineConversionEntry>();
    }

    public static RDStationManager Instance;

    private OfflineDataContainer offlineData;
    private string offlineDataPath;
    private bool isProcessingOfflineData = false;
    private Coroutine autoRetryCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeOfflineStorage();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Tentar processar dados offline ao iniciar
        StartCoroutine(ProcessOfflineDataOnStart());

        // Iniciar corrotina de retry autom�tico
        if (autoRetryCoroutine == null)
        {
            autoRetryCoroutine = StartCoroutine(AutoRetryOfflineData());
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveOfflineData();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveOfflineData();
        }
        else
        {
            // Quando voltar o foco, tentar processar dados offline
            StartCoroutine(ProcessOfflineDataDelayed());
        }
    }

    private void OnDestroy()
    {
        SaveOfflineData();
        if (autoRetryCoroutine != null)
        {
            StopCoroutine(autoRetryCoroutine);
        }
    }

    private void InitializeOfflineStorage()
    {
        // Usar persistentDataPath para Android
        offlineDataPath = Path.Combine(Application.persistentDataPath, OFFLINE_DATA_FILENAME);
        LoadOfflineData();
        LogDebug($"Offline data path: {offlineDataPath}");
    }

    private void LoadOfflineData()
    {
        try
        {
            if (File.Exists(offlineDataPath))
            {
                string jsonContent = File.ReadAllText(offlineDataPath);
                offlineData = JsonConvert.DeserializeObject<OfflineDataContainer>(jsonContent);

                if (offlineData == null)
                {
                    offlineData = new OfflineDataContainer();
                }

                LogDebug($"Carregados {offlineData.pendingConversions.Count} convers�es offline");
            }
            else
            {
                offlineData = new OfflineDataContainer();
                LogDebug("Nenhum arquivo de dados offline encontrado, criando novo");
            }
        }
        catch (Exception ex)
        {
            LogError($"Erro ao carregar dados offline: {ex.Message}");
            offlineData = new OfflineDataContainer();
        }
    }

    private void SaveOfflineData()
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(offlineData, Formatting.Indented);
            File.WriteAllText(offlineDataPath, jsonContent);
            LogDebug($"Dados offline salvos: {offlineData.pendingConversions.Count} convers�es pendentes");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao salvar dados offline: {ex.Message}");
        }
    }

    private IEnumerator ProcessOfflineDataOnStart()
    {
        yield return new WaitForSeconds(2f); // Aguardar inicializa��o completa
        yield return ProcessOfflineData();
    }

    private IEnumerator ProcessOfflineDataDelayed()
    {
        yield return new WaitForSeconds(1f);
        yield return ProcessOfflineData();
    }

    private IEnumerator AutoRetryOfflineData()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoRetryIntervalSeconds);

            if (offlineData.pendingConversions.Count > 0 && !isProcessingOfflineData)
            {
                LogDebug("Auto-retry: Tentando processar dados offline...");
                yield return ProcessOfflineData();
            }
        }
    }

    private IEnumerator ProcessOfflineData()
    {
        if (isProcessingOfflineData || offlineData.pendingConversions.Count == 0)
        {
            yield break;
        }

        isProcessingOfflineData = true;
        LogDebug($"Processando {offlineData.pendingConversions.Count} convers�es offline...");

        List<OfflineConversionEntry> toRemove = new List<OfflineConversionEntry>();

        foreach (var entry in offlineData.pendingConversions)
        {
            if (entry.retryCount >= maxRetryAttempts)
            {
                LogError($"Convers�o {entry.id} excedeu m�ximo de tentativas ({maxRetryAttempts}), removendo...");
                toRemove.Add(entry);
                continue;
            }

            // Verificar se a convers�o n�o � muito antiga (ex: mais de 7 dias)
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTimestamp - entry.timestamp > 604800) // 7 dias em segundos
            {
                LogError($"Convers�o {entry.id} muito antiga, removendo...");
                toRemove.Add(entry);
                continue;
            }

            // Tentar reenviar
            bool success = false;
            yield return StartCoroutine(RetryOfflineConversion(entry, (result) => success = result));

            if (success)
            {
                LogDebug($"Convers�o offline {entry.id} enviada com sucesso!");
                toRemove.Add(entry);
            }
            else
            {
                entry.retryCount++;
                LogDebug($"Falha ao reenviar convers�o {entry.id}, tentativa {entry.retryCount}/{maxRetryAttempts}");
            }

            // Pequena pausa entre tentativas
            yield return new WaitForSeconds(1f);
        }

        // Remover convers�es processadas com sucesso ou que excederam tentativas
        foreach (var entry in toRemove)
        {
            offlineData.pendingConversions.Remove(entry);
        }

        if (toRemove.Count > 0)
        {
            SaveOfflineData();
        }

        isProcessingOfflineData = false;
        LogDebug($"Processamento offline conclu�do. Restam {offlineData.pendingConversions.Count} convers�es pendentes");
    }

    private IEnumerator RetryOfflineConversion(OfflineConversionEntry entry, System.Action<bool> callback)
    {
        JObject conversionData = null;
        bool success = false;

        // Parse JSON outside of try-catch with yield
        try
        {
            conversionData = JObject.Parse(entry.jsonData);
        }
        catch (Exception ex)
        {
            LogError($"Erro ao fazer parse da convers�o {entry.id}: {ex.Message}");
            callback?.Invoke(false);
            yield break;
        }

        // Send conversion (yield allowed here)
        yield return StartCoroutine(SendConversionCoroutine(conversionData,
            onSuccess: () => success = true,
            onError: (error) => success = false,
            isRetry: true));

        callback?.Invoke(success);
    }

    private void StoreOfflineConversion(JObject conversionData)
    {
        try
        {
            // Verificar limite de armazenamento
            if (offlineData.pendingConversions.Count >= maxStoredConversions)
            {
                // Remover a convers�o mais antiga
                offlineData.pendingConversions.RemoveAt(0);
                LogDebug("Limite de armazenamento offline atingido, removendo convers�o mais antiga");
            }

            string jsonData = conversionData.ToString(Formatting.None);
            var offlineEntry = new OfflineConversionEntry(jsonData);
            offlineData.pendingConversions.Add(offlineEntry);

            SaveOfflineData();
            LogDebug($"Convers�o armazenada offline: {offlineEntry.id}");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao armazenar convers�o offline: {ex.Message}");
        }
    }

    /// <summary>
    /// Envia dados de convers�o para RD Station usando JObject
    /// </summary>
    public void SendConversion(JObject conversionData, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            LogError("API Key n�o configurada. Configure sua API Key da RD Station.");
            onError?.Invoke("API Key n�o configurada");
            return;
        }

        // Garantir que conversion_identifier est� definido
        if (conversionData["conversion_identifier"] == null)
            conversionData["conversion_identifier"] = "unity_game_conversion";

        // Validar dados obrigat�rios
        if (!ValidateConversionData(conversionData))
        {
            onError?.Invoke("Dados de convers�o inv�lidos");
            return;
        }

        // Primeiro, tentar processar dados offline pendentes
        StartCoroutine(ProcessOfflineDataBeforeSending(conversionData, onSuccess, onError));
    }

    private IEnumerator ProcessOfflineDataBeforeSending(JObject conversionData, System.Action onSuccess, System.Action<string> onError)
    {
        // Processar dados offline pendentes primeiro
        if (offlineData.pendingConversions.Count > 0 && !isProcessingOfflineData)
        {
            yield return ProcessOfflineData();
        }

        // Agora tentar enviar a nova convers�o
        yield return StartCoroutine(SendConversionCoroutine(conversionData, onSuccess, onError));
    }

    /// <summary>
    /// Envia dados de convers�o usando ConversionData object
    /// </summary>
    public void SendConversion(ConversionData conversionData, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        string jsonString = JsonConvert.SerializeObject(conversionData, Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        JObject jObject = JObject.Parse(jsonString);
        SendConversion(jObject, onSuccess, onError);
    }

    /// <summary>
    /// Cria objeto ConversionData com par�metros espec�ficos
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

    private IEnumerator SendConversionCoroutine(JObject conversionData, System.Action onSuccess, System.Action<string> onError, bool isRetry = false)
    {
        string url = $"{rdStationBaseUrl}{CONVERSION_ENDPOINT}?api_key={apiKey}";

        // Criar payload no formato da API RD Station
        JObject payload = new JObject();

        // Campos obrigat�rios
        payload["conversion_identifier"] = conversionData["conversion_identifier"]?.ToString() ?? "unity_game_conversion";

        if (conversionData["email"] != null)
            payload["email"] = conversionData["email"].ToString();

        // Campos opcionais padr�o
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

        // CORRE��O: Criar o objeto principal com a estrutura correta da API RD Station
        JObject requestBody = new JObject
        {
            ["event_type"] = "CONVERSION",
            ["event_family"] = "CDP",
            ["payload"] = payload
        };

        string jsonPayload = requestBody.ToString(Formatting.None);

        if (!isRetry)
        {
            LogDebug($"Enviando convers�o para RD Station: {jsonPayload}");
        }

        // Criar requisi��o
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Headers corretos
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            // Timeout
            request.timeout = 30;

            yield return request.SendWebRequest();

            // Tratar resposta
            if (request.result == UnityWebRequest.Result.Success)
            {
                LogDebug($"Convers�o enviada com sucesso! Response: {request.downloadHandler.text}");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"Falha ao enviar convers�o. Status: {request.responseCode}, Erro: {request.error}";

                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    errorMessage += $", Response: {request.downloadHandler.text}";
                }

                // Se n�o � um retry, armazenar offline
                if (!isRetry)
                {
                    LogDebug("Armazenando convers�o offline devido � falha de envio");
                    StoreOfflineConversion(conversionData);
                }

                LogError(errorMessage);

                if (!isRetry) // Evitar spam de logs em retries
                {
                    LogError($"URL da requisi��o: {url}");
                    LogError($"Payload enviado: {jsonPayload}");
                    LogError($"API Key configurada: {!string.IsNullOrEmpty(apiKey)}");

                    // Verificar headers de resposta para debug
                    var responseHeaders = request.GetResponseHeaders();
                    if (responseHeaders != null)
                    {
                        LogError($"Response Headers: {string.Join(", ", responseHeaders)}");
                    }
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
    /// Adiciona campo mapeado de m�ltiplas possibilidades
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
    /// Valida dados de convers�o antes do envio
    /// </summary>
    public bool ValidateConversionData(JObject conversionData)
    {
        if (conversionData == null)
        {
            LogError("Dados de convers�o s�o nulos");
            return false;
        }

        // Verificar campos obrigat�rios
        string conversionId = conversionData["conversion_identifier"]?.ToString();
        if (string.IsNullOrEmpty(conversionId))
        {
            LogError("conversion_identifier � obrigat�rio");
            return false;
        }

        string email = conversionData["email"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            LogError("email � obrigat�rio");
            return false;
        }

        // Validar formato do email
        if (!IsValidEmail(email))
        {
            LogError($"Formato de email inv�lido: {email}");
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
        // Teste simples enviando dados m�nimos
        JObject testData = new JObject
        {
            ["conversion_identifier"] = "test_connection",
            ["email"] = "test@example.com"
        };

        bool success = false;
        SendConversion(testData,
            onSuccess: () => { success = true; },
            onError: (error) => {
                LogError($"Teste de conex�o falhou: {error}");
                success = false;
            });

        // Aguardar resultado
        yield return new WaitForSeconds(5f);
        callback?.Invoke(success);
    }

    /// <summary>
    /// For�a o processamento de dados offline pendentes
    /// </summary>
    public void ForceProcessOfflineData()
    {
        StartCoroutine(ProcessOfflineData());
    }

    /// <summary>
    /// Limpa todos os dados offline armazenados
    /// </summary>
    public void ClearOfflineData()
    {
        offlineData.pendingConversions.Clear();
        SaveOfflineData();
        LogDebug("Dados offline limpos");
    }

    /// <summary>
    /// Retorna informa��es sobre dados offline
    /// </summary>
    public int GetPendingConversionsCount()
    {
        return offlineData.pendingConversions.Count;
    }

    /// <summary>
    /// Retorna informa��es detalhadas sobre dados offline
    /// </summary>
    public string GetOfflineDataInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Convers�es pendentes: {offlineData.pendingConversions.Count}");
        sb.AppendLine($"Limite m�ximo: {maxStoredConversions}");
        sb.AppendLine($"Tentativas m�ximas por convers�o: {maxRetryAttempts}");
        sb.AppendLine($"Intervalo de retry autom�tico: {autoRetryIntervalSeconds}s");

        if (offlineData.pendingConversions.Count > 0)
        {
            sb.AppendLine("\nDetalhes das convers�es pendentes:");
            foreach (var conversion in offlineData.pendingConversions)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(conversion.timestamp).ToString("dd/MM/yyyy HH:mm:ss");
                sb.AppendLine($"- ID: {conversion.id.Substring(0, 8)}... | Tentativas: {conversion.retryCount}/{maxRetryAttempts} | Data: {date}");
            }
        }

        return sb.ToString();
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

    #region M�todos de Exemplo

    /// <summary>
    /// Exemplo de uso simples
    /// </summary>
    public void ExampleUsageSimple()
    {
        var conversionData = CreateConversionData(
            conversionIdentifier: "memory_game_completion",
            email: "jogador@exemplo.com",
            name: "Jo�o Silva",
            city: "Crici�ma",
            mobilePhone: "+5548999999999"
        );

        SendConversion(conversionData,
            onSuccess: () => {
                Debug.Log("Convers�o enviada com sucesso!");
            },
            onError: (error) => {
                Debug.LogError($"Erro ao enviar convers�o: {error}");
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
            ["cidade"] = "Crici�ma",           // Ser� mapeado para "city"
            ["telefone"] = "+5548999888777",   // Ser� mapeado para "mobile_phone"
            ["profissao"] = "Designer",        // Ser� mapeado para "job_title"
            ["custom1"] = "Solteiro(a)",       // Ser� mapeado para "cf_estado_civil"
            ["custom2"] = "R$5.000 - R$10.000" // Ser� mapeado para "cf_renda_7_tiers"
        };

        if (ValidateConversionData(conversionData))
        {
            SendConversion(conversionData,
                onSuccess: () => {
                    LogDebug("Convers�o avan�ada enviada!");
                },
                onError: (error) => {
                    LogError($"Falha na convers�o avan�ada: {error}");
                });
        }
    }

    /// <summary>
    /// Exemplo de verifica��o de status offline
    /// </summary>
    public void ExampleCheckOfflineStatus()
    {
        Debug.Log("=== STATUS OFFLINE ===");
        Debug.Log(GetOfflineDataInfo());

        if (GetPendingConversionsCount() > 0)
        {
            Debug.Log("For�ando processamento de dados offline...");
            ForceProcessOfflineData();
        }
    }

    #endregion
}