using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CustomRankingMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _rankingPanel;
    [SerializeField] private GameObject _playerBoxPrefab;
    [SerializeField] private TMP_Text _noRankingText;
    [SerializeField] private TMP_Text _rankingQtt;
    [SerializeField] private Transform _contentPanel;
    [SerializeField] private Button _backButton;

    [Header("Last Player Information")]
    [SerializeField] private GameObject _lastPlayerPanel;
    [SerializeField] private TMP_Text _lastPlayerPosition;
    [SerializeField] private TMP_Text _lastPlayerName;
    [SerializeField] private TMP_Text _lastPlayerPoints;

    [Header("Settings")]
    [SerializeField] private int _maxPlayersToShow = 10;

    // Vari�veis para armazenar as informa��es do �ltimo jogador
    private Dictionary<string, string> _lastPlayer;
    private int _lastPlayerPositionNumber = 0;

    private void Awake()
    {
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(() => AppManager.Instance.OpenMenu("CloseAll"));
        }

        _rankingQtt.text = $"(Exibi��o limitada em at� {_maxPlayersToShow} jogadores)";

        // Inicialmente ocultar o painel do �ltimo jogador
        if (_lastPlayerPanel != null)
            _lastPlayerPanel.SetActive(false);
    }

    public void LoadRanking()
    {
        if (!AppManager.Instance.gameConfig.useLeads)
        {
            _noRankingText.gameObject.SetActive(true);
            return;
        }

        // Limpar objetos anteriores do contentPanel
        foreach (Transform child in _contentPanel)
        {
            Destroy(child.gameObject);
        }

        List<Dictionary<string, string>> players = AppManager.Instance.DataSync.PermanentData;

        if (players == null || players.Count == 0)
        {
            _rankingPanel.SetActive(false);
            if (_noRankingText)
                _noRankingText.gameObject.SetActive(true);

            // N�o h� jogadores, ent�o limpar as refer�ncias do �ltimo jogador
            _lastPlayer = null;
            _lastPlayerPositionNumber = 0;
        }
        else
        {
            _rankingPanel.SetActive(true);
            if (_noRankingText)
                _noRankingText.gameObject.SetActive(false);

            // Obter o �ltimo jogador cadastrado (assume que � o �ltimo na lista)
            _lastPlayer = players.LastOrDefault();

            // Filtrar e ordenar por tempo (ordem crescente - menor tempo � melhor)
            var sortedPlayers = players
                .Where(p => p.ContainsKey("tempo")) // Verificar se o campo 'tempo' existe
                .OrderBy(p => ParseTempo(p["tempo"])) // Ordenar pelo tempo (menor primeiro)
                .ToList();

            // Encontrar a posi��o do �ltimo jogador no ranking
            _lastPlayerPositionNumber = 0;
            if (_lastPlayer != null && _lastPlayer.ContainsKey("tempo") && _lastPlayer.ContainsKey("nome"))
            {
                for (int i = 0; i < sortedPlayers.Count; i++)
                {
                    var p = sortedPlayers[i];
                    if (p.ContainsKey("nome") && p["nome"] == _lastPlayer["nome"] &&
                        p.ContainsKey("tempo") && p["tempo"] == _lastPlayer["tempo"])
                    {
                        _lastPlayerPositionNumber = i + 1; // +1 porque posi��o come�a em 1
                        break;
                    }
                }
            }

            // Preencher os dados do �ltimo jogador nos campos de texto (mas n�o ativar o painel ainda)
            if (_lastPlayer != null && _lastPlayerPositionNumber > 0)
            {
                _lastPlayerPosition.text = _lastPlayerPositionNumber + "�";
                _lastPlayerName.text = _lastPlayer.ContainsKey("nome") ? _lastPlayer["nome"] : "Sem nome";

                // Formatar o tempo para MM:SS
                string tempoFormatado = "--:--";
                if (_lastPlayer.ContainsKey("tempo"))
                {
                    tempoFormatado = FormatarTempoParaMMSS(_lastPlayer["tempo"]);
                }
                _lastPlayerPoints.text = tempoFormatado;
            }

            // Exibir cada jogador no ranking (limitado pela quantidade definida)
            int position = 1;
            foreach (var player in sortedPlayers.Take(_maxPlayersToShow))
            {
                // Instanciar um novo player_box no contentPanel
                GameObject newPlayerBox = Instantiate(_playerBoxPrefab, _contentPanel);

                // Obter as refer�ncias dos campos de texto no prefab
                TMP_Text positionText = newPlayerBox.transform.Find("positionText")?.GetComponent<TMP_Text>();
                TMP_Text nameText = newPlayerBox.transform.Find("text1").GetComponent<TMP_Text>();
                TMP_Text timeText = newPlayerBox.transform.Find("text2").GetComponent<TMP_Text>();

                // Exibir a posi��o, nome e tempo do jogador
                if (positionText != null)
                    positionText.text = position + "�";

                nameText.text = player.ContainsKey("nome") ? player["nome"] : "Sem nome";

                // Formatar o tempo para MM:SS
                string tempoFormatado = FormatarTempoParaMMSS(player["tempo"]);
                timeText.text = tempoFormatado;

                position++;
            }
        }
    }

    // Fun��o para formatar o tempo para o formato MM:SS
    private string FormatarTempoParaMMSS(string tempoString)
    {
        // Se j� estiver no formato MM:SS, verifica se tem zeros � esquerda
        if (tempoString.Contains(":"))
        {
            string[] parts = tempoString.Split(':');
            if (parts.Length == 2)
            {
                // Verifica se consegue converter para inteiros
                if (int.TryParse(parts[0], out int minutos) && int.TryParse(parts[1], out int segundos))
                {
                    // Retorna no formato MM:SS com zeros � esquerda
                    return string.Format("{0:00}:{1:00}", minutos, segundos);
                }
                else
                {
                    // Se n�o conseguir converter, retorna a string original
                    return tempoString;
                }
            }
        }

        // Se for apenas segundos, converte para MM:SS
        if (float.TryParse(tempoString, out float segundosTotais))
        {
            int minutos = Mathf.FloorToInt(segundosTotais / 60);
            int segundos = Mathf.FloorToInt(segundosTotais % 60);
            return string.Format("{0:00}:{1:00}", minutos, segundos);
        }

        // Se n�o conseguir converter, retorna a string original
        return tempoString;
    }

    // Fun��o para converter o tempo no formato MM:SS para segundos para ordena��o correta
    private float ParseTempo(string tempoString)
    {
        if (string.IsNullOrEmpty(tempoString))
            return float.MaxValue; // Valor m�ximo para tempos inv�lidos ou vazios

        // Se o formato for MM:SS
        if (tempoString.Contains(":"))
        {
            string[] parts = tempoString.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int minutes) && int.TryParse(parts[1], out int seconds))
            {
                return minutes * 60 + seconds;
            }
        }

        // Se o formato for apenas segundos
        if (float.TryParse(tempoString, out float segundos))
            return segundos;

        return float.MaxValue; // Valor m�ximo para formatos inv�lidos
    }

    // Fun��o simplificada apenas para mostrar o painel do �ltimo jogador
    public void ShowLastPlayer()
    {
        if (_lastPlayerPanel != null && _lastPlayer != null && _lastPlayerPositionNumber > 0)
        {
            _lastPlayerPanel.SetActive(true);
        }
        else if (_lastPlayerPanel != null)
        {
            _lastPlayerPanel.SetActive(false);
        }
    }
}