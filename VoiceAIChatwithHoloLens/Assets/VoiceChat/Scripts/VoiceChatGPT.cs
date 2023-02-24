using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class VoiceChatGPT : MonoBehaviour
{
    [SerializeField] private Text inText;
    [SerializeField] private Text outText;
    [SerializeField] private UnityEvent<string> OnResult = new UnityEvent<string>();
    [Space(14)]
    [SerializeField] private string debugText = "";

    private GPTControl gpt = new GPTControl();

    // Start is called before the first frame update
    void Start()
    {
        gpt.messageReceivedCallback += (result) =>
        {
            outText.text = result;
            OnResult?.Invoke(result);
        };
        gpt.ResetGPT();
    }

    public void SetText(string text) => inText.text = text;

    public void SendChatGPT() => gpt.RunChatGPT(this, inText.text);

    public void ResetGPT() => gpt.ResetGPT();

    [ContextMenu("GPT/Debug")]
    public void DebugGPT() => gpt.RunChatGPT(this, debugText);
}

public class GPTControl
{
    public Action<string> messageReceivedCallback;

    private string endpoint = "https://api.openai.com/v1/completions";
    private string apikey = "<set API key>";
    private string chatLog;

    public void ResetGPT() => chatLog = "あなたはどんな質問にも1文で回答できる人工知能です。";

    public void RunChatGPT(MonoBehaviour mono, string text = null)
    {
        Debug.Log($"start chatgpt {text}");
        chatLog += text;
        chatLog += "\n";
        mono.StartCoroutine(SendChatGPT(chatLog, result =>
        {
            messageReceivedCallback?.Invoke(result);
            Debug.Log("end chatgpt " + result);
            chatLog += result;
            chatLog += "\n\n";
        }));
    }

    private IEnumerator SendChatGPT(string text, Action<string> resultText)
    {
        var requestData = new APIRequestData() { Prompt = text };
        var json = JsonConvert.SerializeObject(requestData, Formatting.Indented);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        using (var request = UnityWebRequest.Post(endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apikey);
            yield return request.SendWebRequest();
            while (true)
            {
                if (request.result == UnityWebRequest.Result.InProgress)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    break;
                }
            }
            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                var responsedata = JsonConvert.DeserializeObject<APIResponseData>(result);
                Debug.Log(result);
                resultText?.Invoke(responsedata.Choices.FirstOrDefault().Text);
                yield break;
            }
            else
            {
                Debug.LogError(request.error);
            }
        }
        resultText?.Invoke(null);
    }

    #region JsonRequest
    [JsonObject]
    public class APIRequestData
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "text-davinci-003";
        [JsonProperty("prompt")]
        public string Prompt { get; set; } = "";
        [JsonProperty("temperature")]
        public int Temperature { get; set; } = 0;
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 500;
    }

    /// <summary>
    /// APIレスポンス
    /// 
    /// https://beta.openai.com/docs/api-reference/authentication
    /// </summary>
    [JsonObject]
    public class APIResponseData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("object")]
        public string Object { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("created")]
        public int Created { get; set; }
        [JsonProperty("choices")]
        public ChoiceData[] Choices { get; set; }
        [JsonProperty("usage")]
        public UsageData Usage { get; set; }
    }

    [JsonObject]
    public class UsageData
    {
        [JsonProperty("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonProperty("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonProperty("total_tokens")]
        public int TotalTokens { get; set; }
    }

    [JsonObject]
    public class ChoiceData
    {
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("logprobs")]
        public string Logprobs { get; set; }
        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }
    #endregion
}
