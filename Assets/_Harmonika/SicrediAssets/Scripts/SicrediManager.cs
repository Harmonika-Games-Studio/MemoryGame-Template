using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class SicrediManager : MonoBehaviour {

    [SerializeField] private Button _convertToCsvButton;

    private void Start() {

        Setup();
    }

    private void Setup() {

        _convertToCsvButton.onClick.AddListener(ConvertPermanentJsonToCsv);
    }

    public void ConvertPermanentJsonToCsv() {

        List<Dictionary<string, string>> permanentData = AppManager.Instance.DataSync.PermanentData;
        if (permanentData == null || permanentData.Count == 0) {
            Debug.LogWarning("Nenhum dado encontrado no arquivo permanente.");
            return;
        }

        string csvPath;
        string directory;

        if (Application.platform == RuntimePlatform.Android) {
            // Android path handling remains the same
            try {
                // Option 1: Save to app's external files directory (more reliable)
                directory = Application.persistentDataPath;

                // Option 2: Save to shared Documents folder
                using (AndroidJavaClass environmentClass = new AndroidJavaClass("android.os.Environment")) {
                    using (AndroidJavaObject documentsDirectory = environmentClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory",
                           environmentClass.GetStatic<string>("DIRECTORY_DOCUMENTS"))) {
                        directory = documentsDirectory.Call<string>("getAbsolutePath");
                    }
                }

                csvPath = Path.Combine(directory, "Leads.csv");
            }
            catch (System.Exception e) {
                Debug.LogError("Erro ao acessar diretório de documentos no Android: " + e.Message);
                AppManager.Instance.ShowLeadsMessage($"Erro ao salvar CSV: Não foi possível acessar diretório de documentos");
                directory = Application.persistentDataPath;
                csvPath = Path.Combine(directory, "Leads.csv");
            }
        } else {
            // Fix for Windows and other platforms
            try {
                // For Windows standalone builds, Application.dataPath might point to inside the .exe
                // Using a more reliable location - Environment.GetFolderPath for Documents folder
                directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Create a subfolder for your app (optional)
                string appFolder = Path.Combine(directory, Application.productName);
                if (!Directory.Exists(appFolder)) {
                    Directory.CreateDirectory(appFolder);
                }

                directory = appFolder;
                csvPath = Path.Combine(directory, "Leads.csv");

                // Log path for debugging
                Debug.Log("Trying to save CSV to: " + csvPath);
            }
            catch (System.Exception e) {
                Debug.LogError("Erro ao definir caminho para CSV: " + e.Message);

                AppManager.Instance.ShowLeadsMessage($"Erro ao salvar CSV: Não foi possível definir o caminho");

                // Fallback to persistent data path if there's an error
                directory = Application.persistentDataPath;
                csvPath = Path.Combine(directory, "Leads.csv");
                Debug.Log("Fallback path: " + csvPath);
            }
        }

        try {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            // Use a try-catch block for the file writing operation
            using (StreamWriter writer = new StreamWriter(csvPath)) {
                // Obter os cabeçalhos
                var headers = permanentData.First().Keys;
                writer.WriteLine(string.Join(",", headers));

                // Escrever os dados
                foreach (var entry in permanentData) {
                    writer.WriteLine(string.Join(",", entry.Values));
                }

                // Flush and close the writer explicitly
                writer.Flush();
            }

            AppManager.Instance.ShowLeadsMessage($"Dados convertidos para .csv com sucesso! Cheque sua pasta de Documentos.");

            Debug.Log("Dados convertidos para CSV com sucesso: " + csvPath);
        }
        catch (System.Exception e) {
            Debug.LogError("Erro ao salvar CSV: " + e.Message + "\nStack Trace: " + e.StackTrace);
            AppManager.Instance.ShowLeadsMessage($"Erro ao salvar CSV.");
        }

        // Only open file explorer if on Windows
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer) {
            OpenExplorerWindow(directory);
        }
    }

    private void OpenExplorerWindow(string directoryPath) {
        directoryPath = Path.GetFullPath(directoryPath);

        if (Directory.Exists(directoryPath)) {
            try {
                // Open the directory without selecting any file
                System.Diagnostics.Process.Start("explorer.exe", "\"" + directoryPath + "\"");
            }
            catch (System.Exception e) {
                Debug.LogError("Erro ao abrir o explorador de arquivos: " + e.Message);
            }
        } else {
            Debug.LogWarning("Diretório não encontrado: " + directoryPath);
        }
    }
}