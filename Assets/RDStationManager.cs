using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [SerializeField] private int maxStoredConversions = 100; // Máximo de conversões armazenadas offline
    [SerializeField] private float autoRetryIntervalSeconds = 30f; // Intervalo para tentar reenviar dados pendentes

    // Endpoint correto para conversões
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
        public bool sentSuccessfully;
        public long lastRetryTimestamp;
        public int initialFailCount;

        public OfflineConversionEntry(string data)
        {
            jsonData = data;
            retryCount = 0;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            id = Guid.NewGuid().ToString();
            sentSuccessfully = false;
            lastRetryTimestamp = 0;
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

        // Iniciar corrotina de retry automático
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

                LogDebug($"Carregados {offlineData.pendingConversions.Count} conversões offline");
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
            LogDebug($"Dados offline salvos: {offlineData.pendingConversions.Count} conversões pendentes");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao salvar dados offline: {ex.Message}");
        }
    }

    private IEnumerator ProcessOfflineDataOnStart()
    {
        yield return new WaitForSeconds(2f); // Aguardar inicialização completa
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

            int unsentCount = GetUnsentConversionsCount();
            if (unsentCount > 0 && !isProcessingOfflineData)
            {
                LogDebug($"Auto-retry: Processando {unsentCount} conversões offline pendentes...");
                yield return StartCoroutine(ProcessOfflineData());

                int remainingUnsent = GetUnsentConversionsCount();
                if (remainingUnsent != unsentCount)
                {
                    LogDebug($"Auto-retry resultado: {unsentCount - remainingUnsent} enviadas, {remainingUnsent} restam");
                }
            }
        }
    }

    public void ProcessPendingDataNow()
    {
        int unsentCount = GetUnsentConversionsCount();
        if (unsentCount > 0)
        {
            LogDebug($"Processando manualmente {unsentCount} conversões pendentes...");
            StartCoroutine(ProcessOfflineData());
        }
        else
        {
            LogDebug("Nenhuma conversão pendente para processar");
        }
    }

    private IEnumerator ProcessOfflineData()
    {
        if (isProcessingOfflineData)
        {
            LogDebug("Já processando dados offline, pulando...");
            yield break;
        }

        // Get only unsent conversions
        var unsentConversions = offlineData.pendingConversions
            .Where(c => !c.sentSuccessfully)
            .ToList();

        if (unsentConversions.Count == 0)
        {
            LogDebug("Nenhuma conversão pendente para processar");
            yield break;
        }

        isProcessingOfflineData = true;
        LogDebug($"Processando {unsentConversions.Count} conversões não enviadas...");

        int successCount = 0;
        int failureCount = 0;

        foreach (var entry in unsentConversions)
        {
            //// Check if we haven't exceeded OFFLINE retry attempts
            //if (entry.retryCount >= maxRetryAttempts)
            //{
            //    LogDebug($"Conversão {entry.id} já excedeu tentativas de retry offline ({entry.retryCount}/{maxRetryAttempts}). Total de falhas: inicial({entry.initialFailCount}) + offline({entry.retryCount})");
            //    continue;
            //}

            // Check if enough time has passed since last retry
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (entry.lastRetryTimestamp > 0 && (currentTimestamp - entry.lastRetryTimestamp) < retryDelaySeconds)
            {
                LogDebug($"Conversão {entry.id} ainda está em cooldown, pulando...");
                continue;
            }

            // Try to send the conversion
            bool success = false;
            entry.lastRetryTimestamp = currentTimestamp;
            entry.retryCount++;

            yield return StartCoroutine(RetryOfflineConversion(entry, (result) => success = result));

            if (success)
            {
                LogDebug($"✓ Conversão offline {entry.id} enviada com sucesso! (após {entry.retryCount} tentativas offline)");
                entry.sentSuccessfully = true;
                successCount++;
            }
            else
            {
                LogDebug($"✗ Falha ao reenviar conversão {entry.id}, tentativa offline {entry.retryCount}/{maxRetryAttempts} (total de falhas: {entry.initialFailCount + entry.retryCount})");
                failureCount++;
            }

            // Small pause between attempts to avoid overwhelming the API
            yield return new WaitForSeconds(1f);
        }

        // Always save the current state
        SaveOfflineData();

        isProcessingOfflineData = false;
        int remainingUnsent = GetUnsentConversionsCount();
        LogDebug($"Processamento concluído. Sucessos: {successCount}, Falhas: {failureCount}, Restam: {remainingUnsent} não enviadas");
    }

    private IEnumerator RetryOfflineConversion(OfflineConversionEntry entry, System.Action<bool> callback)
    {
        JObject conversionData = null;

        try
        {
            conversionData = JObject.Parse(entry.jsonData);
        }
        catch (Exception ex)
        {
            LogError($"Erro ao fazer parse da conversão {entry.id}: {ex.Message}");
            callback?.Invoke(false);
            yield break;
        }

        bool success = false;
        bool requestCompleted = false;

        // Use a coroutine with callback to handle the async nature properly
        yield return StartCoroutine(SendConversionCoroutine(conversionData,
            onSuccess: () => {
                success = true;
                requestCompleted = true;
            },
            onError: (error) => {
                success = false;
                requestCompleted = true;
                LogDebug($"Retry falhou para {entry.id}: {error}");
            },
            isRetry: true));

        // Wait a bit more if needed to ensure request completed
        float waitTime = 0f;
        while (!requestCompleted && waitTime < 30f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        if (!requestCompleted)
        {
            LogError($"Timeout ao tentar reenviar conversão {entry.id}");
            success = false;
        }

        callback?.Invoke(success);
    }

    private void StoreOfflineConversion(JObject conversionData)
    {
        try
        {
            // Verificar limite de armazenamento apenas para conversões não enviadas
            int unsentCount = GetUnsentConversionsCount();
            if (unsentCount >= maxStoredConversions)
            {
                // Tentar encontrar conversões muito antigas que foram enviadas com sucesso para remover
                var oldSentConversions = offlineData.pendingConversions
                    .Where(c => c.sentSuccessfully)
                    .OrderBy(c => c.timestamp)
                    .ToList();

                if (oldSentConversions.Count > 0)
                {
                    // Remover apenas as conversões enviadas mais antigas se necessário
                    int toRemove = Math.Min(oldSentConversions.Count, unsentCount - maxStoredConversions + 1);
                    for (int i = 0; i < toRemove; i++)
                    {
                        offlineData.pendingConversions.Remove(oldSentConversions[i]);
                        LogDebug($"Removendo conversão enviada antiga para liberar espaço: {oldSentConversions[i].id}");
                    }
                }
                else
                {
                    LogError($"Limite de armazenamento offline atingido ({maxStoredConversions}) e não há conversões enviadas antigas para remover. Armazenando mesmo assim para não perder dados.");
                }
            }

            string jsonData = conversionData.ToString(Formatting.None);
            var offlineEntry = new OfflineConversionEntry(jsonData);
            offlineData.pendingConversions.Add(offlineEntry);

            SaveOfflineData();
            LogDebug($"Conversão armazenada offline: {offlineEntry.id}");
        }
        catch (Exception ex)
        {
            LogError($"Erro ao armazenar conversão offline: {ex.Message}");
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

        // Sempre tentar processar dados offline pendentes antes de enviar novos dados
        StartCoroutine(ProcessOfflineDataBeforeSending(conversionData, onSuccess, onError));
    }

    private IEnumerator ProcessOfflineDataBeforeSending(JObject conversionData, System.Action onSuccess, System.Action<string> onError)
    {
        // Always try to process offline data first if we have any unsent conversions
        int unsentCount = GetUnsentConversionsCount();
        if (unsentCount > 0 && !isProcessingOfflineData)
        {
            LogDebug($"Processando {unsentCount} conversões offline pendentes antes de enviar nova conversão...");
            yield return StartCoroutine(ProcessOfflineData());

            // Log results after processing
            int remainingUnsent = GetUnsentConversionsCount();
            if (remainingUnsent < unsentCount)
            {
                LogDebug($"Enviadas {unsentCount - remainingUnsent} conversões offline. Restam {remainingUnsent}");
            }
        }

        // Now try to send the new conversion
        yield return StartCoroutine(SendConversionCoroutine(conversionData, onSuccess, onError));
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

    private IEnumerator SendConversionCoroutine(JObject conversionData, System.Action onSuccess, System.Action<string> onError, bool isRetry = false)
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

        if (!isRetry)
        {
            LogDebug($"Enviando conversão para RD Station: {jsonPayload}");
        }

        // Criar requisição
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

                // Se não é um retry, armazenar offline
                if (!isRetry)
                {
                    LogDebug("Armazenando conversão offline devido à falha de envio");
                    StoreOfflineConversion(conversionData);
                }

                LogError(errorMessage);

                if (!isRetry) // Evitar spam de logs em retries
                {
                    LogError($"URL da requisição: {url}");
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

    /// <summary>
    /// Força o processamento de dados offline pendentes (incluindo os que excederam tentativas)
    /// </summary>
    public void ForceProcessAllOfflineData()
    {
        StartCoroutine(ForceProcessAllOfflineDataCoroutine());
    }

    private IEnumerator ForceProcessAllOfflineDataCoroutine()
    {
        if (isProcessingOfflineData)
        {
            LogDebug("Já processando dados offline, aguardando...");
            yield break;
        }

        LogDebug("Forçando processamento de TODOS os dados offline, incluindo os que excederam tentativas...");

        // Resetar contadores de retry para tentar novamente
        foreach (var entry in offlineData.pendingConversions)
        {
            if (!entry.sentSuccessfully)
            {
                entry.retryCount = 0; // Resetar para tentar novamente
                entry.lastRetryTimestamp = 0;
                LogDebug($"Resetando tentativas para conversão {entry.id}");
            }
        }

        // Processar todos os dados
        yield return ProcessOfflineData();
    }

    /// <summary>
    /// Força o processamento apenas de dados offline que ainda não excedaram tentativas
    /// </summary>
    public void ForceProcessOfflineData()
    {
        StartCoroutine(ProcessOfflineData());
    }

    /// <summary>
    /// Limpa apenas conversões que foram enviadas com sucesso
    /// </summary>
    public void ClearSentConversions()
    {
        int initialCount = offlineData.pendingConversions.Count;
        offlineData.pendingConversions.RemoveAll(c => c.sentSuccessfully);
        int removedCount = initialCount - offlineData.pendingConversions.Count;

        SaveOfflineData();
        LogDebug($"Removidas {removedCount} conversões enviadas com sucesso. Restam {offlineData.pendingConversions.Count} não enviadas");
    }

    /// <summary>
    /// MÉTODO PERIGOSO: Limpa TODOS os dados offline - usar apenas em casos extremos
    /// </summary>
    public void ClearAllOfflineData()
    {
        LogError("ATENÇÃO: Limpando TODOS os dados offline - isso pode causar perda de dados!");
        offlineData.pendingConversions.Clear();
        SaveOfflineData();
        LogDebug("TODOS os dados offline foram limpos");
    }

    /// <summary>
    /// Retorna informações sobre dados offline
    /// </summary>
    public int GetPendingConversionsCount()
    {
        return offlineData.pendingConversions.Count;
    }

    /// <summary>
    /// Retorna apenas conversões que ainda não foram enviadas
    /// </summary>
    public int GetUnsentConversionsCount()
    {
        return offlineData.pendingConversions.Count(c => !c.sentSuccessfully);
    }

    /// <summary>
    /// Retorna conversões que foram enviadas com sucesso
    /// </summary>
    public int GetSentConversionsCount()
    {
        return offlineData.pendingConversions.Count(c => c.sentSuccessfully);
    }

    /// <summary>
    /// Retorna conversões que excederam tentativas mas ainda não foram enviadas
    /// </summary>
    public int GetFailedConversionsCount()
    {
        return offlineData.pendingConversions.Count(c => !c.sentSuccessfully && c.retryCount >= maxRetryAttempts);
    }

    /// <summary>
    /// Retorna informações detalhadas sobre dados offline
    /// </summary>
    public string GetOfflineDataInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== STATUS OFFLINE STORAGE ===");
        sb.AppendLine($"Total de conversões armazenadas: {offlineData.pendingConversions.Count}");
        sb.AppendLine($"Conversões enviadas com sucesso: {GetSentConversionsCount()}");
        sb.AppendLine($"Conversões não enviadas: {GetUnsentConversionsCount()}");
        sb.AppendLine($"Conversões que excederam tentativas: {GetFailedConversionsCount()}");
        sb.AppendLine($"Limite máximo: {maxStoredConversions}");
        sb.AppendLine($"Tentativas máximas por conversão: {maxRetryAttempts}");
        sb.AppendLine($"Intervalo de retry automático: {autoRetryIntervalSeconds}s");

        if (offlineData.pendingConversions.Count > 0)
        {
            sb.AppendLine("\n=== DETALHES DAS CONVERSÕES ===");
            foreach (var conversion in offlineData.pendingConversions)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(conversion.timestamp).ToString("dd/MM/yyyy HH:mm:ss");
                string status = conversion.sentSuccessfully ? "✓ ENVIADA" :
                               conversion.retryCount >= maxRetryAttempts ? "✗ FALHARAM TODAS TENTATIVAS" :
                               "⏳ PENDENTE";

                sb.AppendLine($"- ID: {conversion.id.Substring(0, 8)}... | Status: {status} | Tentativas: {conversion.retryCount}/{maxRetryAttempts} | Data: {date}");
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

    /// <summary>
    /// Exemplo de verificação de status offline
    /// </summary>
    public void ExampleCheckOfflineStatus()
    {
        Debug.Log(GetOfflineDataInfo());

        if (GetUnsentConversionsCount() > 0)
        {
            Debug.Log("Forçando processamento de dados offline pendentes...");
            ForceProcessOfflineData();
        }

        if (GetFailedConversionsCount() > 0)
        {
            Debug.Log($"Existem {GetFailedConversionsCount()} conversões que falharam todas as tentativas.");
            Debug.Log("Use ForceProcessAllOfflineData() para tentar reenviar todas, incluindo as que falharam.");
        }
    }

    /// <summary>
    /// Exemplo de limpeza de dados enviados com sucesso
    /// </summary>
    public void ExampleCleanupSentData()
    {
        Debug.Log("=== LIMPEZA DE DADOS ENVIADOS ===");
        Debug.Log($"Conversões enviadas com sucesso: {GetSentConversionsCount()}");

        if (GetSentConversionsCount() > 0)
        {
            Debug.Log("Removendo conversões enviadas com sucesso para liberar espaço...");
            ClearSentConversions();
            Debug.Log("Limpeza concluída!");
        }
        else
        {
            Debug.Log("Nenhuma conversão enviada para limpar.");
        }
    }

    /// <summary>
    /// Exemplo de retry forçado para todas as conversões
    /// </summary>
    public void ExampleForceRetryAll()
    {
        Debug.Log("=== RETRY FORÇADO DE TODAS AS CONVERSÕES ===");
        int failedCount = GetFailedConversionsCount();

        if (failedCount > 0)
        {
            Debug.Log($"Tentando reenviar {failedCount} conversões que falharam anteriormente...");
            ForceProcessAllOfflineData();
        }
        else
        {
            Debug.Log("Não há conversões que falharam para tentar novamente.");
        }
    }

    #endregion
}