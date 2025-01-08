using Harmonika.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class WebGLNetworking : MonoBehaviour
{
    private static WebGLNetworking _instance;
    [HideInInspector] public string jsonData;

    #region Properties
    /// <summary>
    /// Returns WebGLNetworking's single static instance.
    /// </summary>
    public static WebGLNetworking Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<WebGLNetworking>();

                if (_instance == null)
                {
                    Debug.LogError("Houve uma tentativa de acesso a uma instância vazia do WebGLNetworking, mas o objeto não está na cena." +
                        "\nPor favor adicione o WebGLNetworking na cena antes de continuar!");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Returns a test mocked json as a string.
    /// </summary>
    public string TestJson
    {
        get
        {
            List<StorageItemConfig> storageItems = new List<StorageItemConfig>
            {
                new() { _itemName = "Item1", _initialValue = 10, _prizeScore = 100 },
                new() { _itemName = "Item2", _initialValue = 5, _prizeScore = 50 }
            };

            JObject rawData = new JObject
            {
                { "cardBack", "https://i.imgur.com/LDsqclp.png" },
                { "cardsList", new JArray
                    {
                        "https://draftsim.com/wp-content/uploads/2022/07/dmu-281-forest.png",
                        "https://draftsim.com/wp-content/uploads/2022/07/dmu-278-island.png",
                        "https://draftsim.com/wp-content/uploads/2022/07/dmu-280-mountain.png",
                        "https://draftsim.com/wp-content/uploads/2022/07/dmu-277-plains.png",
                        "https://draftsim.com/wp-content/uploads/2022/07/dmu-279-swamp.png",
                        "https://mtginsider.com/wp-content/uploads/2024/08/senseisdiviningtop.png"
                    }
                },
                { "storageItems", JArray.FromObject(storageItems) },
                { "useLeads", false},
                { "gameName", "Jogo da <b>memória</b>"}
            };

            return rawData.ToString();
        }
    }
    #endregion

    private void Awake()
    {
        #region Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        #endregion
    }

    public void ReceiveJson(string json)
    {
        jsonData = json;
        Debug.Log("WebGLNetworking -> ReceiveJson: Recebido" + json);
    }

    public IEnumerator DownloadImageRoutine(string url, UnityAction<Sprite> onDownloaded)
    {
        Debug.Log("Downloading Image" + url);
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            // Envie a solicitação e aguarde a resposta
            yield return webRequest.SendWebRequest();

            // Verifique se houve erro
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"WebGLNetworking -> DownloadImageToCardRoutine: Erro ao baixar a imagem: " + webRequest.error);
            }
            else
            {
                onDownloaded?.Invoke(null);
                // Converta a textura baixada em um sprite
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                onDownloaded?.Invoke(sprite);
            }
        }
    }


    /*public IEnumerator DownloadCardsListRoutine(string[] urls, Action<Sprite[]> callback)
    {
        Sprite[] newSpriteArray = new Sprite[urls.Length];

        for (int i = 0; i < urls.Length; i++)
        {
            yield return StartCoroutine(DownloadImageRoutine(urls[i], (sprite) => newSpriteArray[i] = sprite));
        }
        callback.Invoke(newSpriteArray);
    }*/
}