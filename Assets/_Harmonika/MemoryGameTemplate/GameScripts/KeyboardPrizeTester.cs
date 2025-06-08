using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardPrizeTester : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private bool showStockInfo = true;

    void Update()
    {
        if (!enableTesting || !Application.isPlaying) return;

        // Detecta teclas num�ricas 0-7 usando novo Input System
        if (Keyboard.current.digit0Key.wasPressedThisFrame) TestPrizeIndex(0);
        if (Keyboard.current.digit1Key.wasPressedThisFrame) TestPrizeIndex(1);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) TestPrizeIndex(2);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) TestPrizeIndex(3);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) TestPrizeIndex(4);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) TestPrizeIndex(5);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) TestPrizeIndex(6);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) TestPrizeIndex(7);

        // Tecla S para mostrar estoque
        if (Keyboard.current.sKey.wasPressedThisFrame) ShowCurrentStock();

        // Tecla H para mostrar ajuda
        if (Keyboard.current.hKey.wasPressedThisFrame) ShowHelp();
    }

    void TestPrizeIndex(int targetIndex)
    {
        if (AppManager.Instance == null || AppManager.Instance.Storage == null)
        {
            Debug.LogError("[PRIZE TEST] AppManager ou Storage n�o encontrado!");
            return;
        }

        Debug.Log($"[PRIZE TEST] ========== TESTANDO INDEX {targetIndex} ==========");

        // Mostra estoque atual do �ndice alvo
        if (showStockInfo && targetIndex < AppManager.Instance.Storage.ItemsList.Count)
        {
            var targetItem = AppManager.Instance.Storage.ItemsList[targetIndex];
            Debug.Log($"[PRIZE TEST] Index {targetIndex}: {targetItem.ItemName} - Estoque: {targetItem.Quantity}");
        }

        // Executa o teste com fallback
        string prize = TryGetPrizeWithFallback(targetIndex);

        if (prize != null)
        {
            Debug.Log($"[PRIZE TEST] ? SUCESSO! Pr�mio obtido: {prize}");
        }
        else
        {
            Debug.Log($"[PRIZE TEST] ? FALHOU! Nenhum pr�mio dispon�vel (�ndices 0-{targetIndex} sem estoque)");
        }

        Debug.Log($"[PRIZE TEST] =====================================");
    }

    // Fun��o auxiliar para verificar se um �ndice tem estoque
    bool HasStock(int index)
    {
        return index >= 0 && index < AppManager.Instance.Storage.ItemsList.Count &&
               AppManager.Instance.Storage.ItemsList[index].Quantity > 0;
    }

    // Fun��o auxiliar para tentar pegar um pr�mio, com fallback para �ndices menores
    string TryGetPrizeWithFallback(int targetIndex)
    {
        Debug.Log($"[PRIZE TEST] Tentando �ndices de {targetIndex} at� 0...");

        for (int i = targetIndex; i >= 0; i--)
        {
            if (HasStock(i))
            {
                var item = AppManager.Instance.Storage.ItemsList[i];
                Debug.Log($"[PRIZE TEST] ? Index {i} ({item.ItemName}) tem estoque ({item.Quantity}), obtendo pr�mio...");
                return AppManager.Instance.Storage.GetSpecificPrize(i);
            }
            else
            {
                var item = AppManager.Instance.Storage.ItemsList[i];
                Debug.Log($"[PRIZE TEST] ? Index {i} ({item.ItemName}) SEM estoque ({item.Quantity}), tentando pr�ximo...");
            }
        }

        Debug.Log($"[PRIZE TEST] ? Nenhum �ndice de 0 a {targetIndex} possui estoque!");
        return null; // Nenhum item dispon�vel
    }

    void ShowCurrentStock()
    {
        if (AppManager.Instance == null || AppManager.Instance.Storage == null)
        {
            Debug.LogError("[STOCK] AppManager ou Storage n�o encontrado!");
            return;
        }

        Debug.Log("[STOCK] ========== ESTOQUE ATUAL ==========");

        for (int i = 0; i < AppManager.Instance.Storage.ItemsList.Count; i++)
        {
            var item = AppManager.Instance.Storage.ItemsList[i];
            string status = item.Quantity > 0 ? "?" : "?";
            Debug.Log($"[STOCK] {status} Index {i}: {item.ItemName} - Quantidade: {item.Quantity}");
        }

        Debug.Log($"[STOCK] Total de itens no invent�rio: {AppManager.Instance.Storage.InventoryCount}");
        Debug.Log("[STOCK] =====================================");
    }

    void ShowHelp()
    {
        Debug.Log("========== COMANDOS DE TESTE ==========");
        Debug.Log("Teclas 0-7: Testa pr�mio do �ndice espec�fico (com fallback)");
        Debug.Log("Tecla S: Mostra estoque atual");
        Debug.Log("Tecla H: Mostra esta ajuda");
        Debug.Log("======================================");
    }

    void Start()
    {
        if (enableTesting)
        {
            ShowHelp();
        }
    }
}

// VERS�O ALTERNATIVA: Se n�o quiser usar Input System, volte para Input cl�ssico
/*
// No Player Settings > XR Plug-in Management > Input Handling: 
// Mude de "Input System Package" para "Both" ou "Input Manager (Old)"

// Ou use esta vers�o h�brida que funciona com ambos:
public class KeyboardPrizeTesterCompatible : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableTesting = true;

    void Update()
    {
        if (!enableTesting || !Application.isPlaying) return;

        // Vers�o compat�vel que funciona com Input System ou Input cl�ssico
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame) TestPrizeIndex(0);
            if (Keyboard.current.digit1Key.wasPressedThisFrame) TestPrizeIndex(1);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) TestPrizeIndex(2);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) TestPrizeIndex(3);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) TestPrizeIndex(4);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) TestPrizeIndex(5);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) TestPrizeIndex(6);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) TestPrizeIndex(7);
            if (Keyboard.current.sKey.wasPressedThisFrame) ShowCurrentStock();
        }
        #else
        // Fallback para Input cl�ssico se Input System n�o estiver dispon�vel
        if (Input.GetKeyDown(KeyCode.Alpha0)) TestPrizeIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestPrizeIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestPrizeIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestPrizeIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestPrizeIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestPrizeIndex(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) TestPrizeIndex(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) TestPrizeIndex(7);
        if (Input.GetKeyDown(KeyCode.S)) ShowCurrentStock();
        #endif
    }

    // Resto das fun��es permanecem iguais...
}
*/