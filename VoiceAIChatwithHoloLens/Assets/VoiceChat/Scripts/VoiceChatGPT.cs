using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class VoiceChatGPT : MonoBehaviour
{
    [SerializeField] private UnityEvent<string> OnResult = new UnityEvent<string>();
    [SerializeField] private UnityEvent OnSmile = new UnityEvent();
    [SerializeField] private UnityEvent OnAnger = new UnityEvent();
    [SerializeField] private UnityEvent OnConfidence = new UnityEvent();
    [SerializeField] private UnityEvent OnSap = new UnityEvent();
    [SerializeField] private UnityEvent OnOther = new UnityEvent();
    [Space(14)]
    [SerializeField] private string debugText = "";

    private static readonly string apikey = "<api_key>";
    private static readonly string initMessage = @"以下の条件に従って、疑似的な感情パラメーターをもつチャットボットとしてロールプレイをします。
感情パラメーターは会話を通じて変動します。
感情パラメーターには喜び、怒り、自信、驚きが含まれています。
各感情パラメーターは0から5の間で変化します。
会話は感情パラメーターの値によって返答は変化します。
以後の会話は以下のフォーマットで行う。
{""感情パラメーター"":{""喜び"":0,""怒り"":0,""自信"":0,""驚き"":0},""会話"":""会話内容""}
";

    private IGPTControl gpt = new ChatGPTControl();

    // Start is called before the first frame update
    void Start()
    {
        gpt.Init(apikey, initMessage);
        gpt.messageReceivedCallback += (result) =>
        {
            Debug.Log("assistant : " + result);
            var message = JsonConvert.DeserializeObject<MessageParameter>(result);
            OnResult?.Invoke(message.message);
            switch (message.parameter.GetParam())
            {
                case Emotion.smile:
                    OnSmile?.Invoke();
                    break;
                case Emotion.anger:
                    OnAnger?.Invoke();
                    break;
                case Emotion.confidence:
                    OnConfidence?.Invoke();
                    break;
                case Emotion.sap:
                    OnSap?.Invoke();
                    break;
                default:
                    OnOther?.Invoke();
                    break;
            }
        };
    }

    public void SendChatGPT(string text)
    {
        Debug.Log($"user : {text}"); 
        gpt.Run(this, text);
    }

    [ContextMenu("GPT/Debug")]
    public void DebugGPT()
    {
        Start();
        Debug.Log($"user : {debugText}");
        gpt.Run(this, debugText);
    }

    #region MessageParameter
    [JsonObject]
    public class MessageParameter
    {
        [JsonProperty("感情パラメーター")]
        public Parameter parameter { get; set; }
        [JsonProperty("会話")]
        public string message { get; set; }
    }

    [JsonObject]
    public class Parameter
    {
        [JsonProperty("喜び")]
        public int smile { get; set; }
        [JsonProperty("怒り")]
        public int anger { get; set; }
        [JsonProperty("自信")]
        public int confidence { get; set; }
        [JsonProperty("驚き")]
        public int sap { get; set; }

        public Emotion GetParam()
        {
            var num = new[] { smile, anger, confidence, sap };
            var index = Array.IndexOf(num, num.Max());
            switch (index)
            {
                case 0: return Emotion.smile;
                case 1: return Emotion.anger;
                case 2: return Emotion.confidence;
                case 3: return Emotion.sap;
                default: return Emotion.none;
            }
        }
    }

    public enum Emotion
    {
        none,
        smile,
        anger,
        confidence,
        sap
    }
    #endregion
}

public interface IGPTControl
{
    public void Init(string api_key = null, string init_text = null);
    public event Action<string> messageReceivedCallback;
    public void Run(MonoBehaviour mono, string text);
}

public class baseGPTControl
{
    private string endpoint;
    private string api_key;
    private string init_text;

    protected baseGPTControl(string endpoint) { this.endpoint = endpoint; }

    protected string baseInit(string api_key = null, string init_text = null)
    {
        if (api_key != null) this.api_key = api_key;
        if (init_text != null) this.init_text = init_text;
        return this.init_text;
    }

    protected void baseRun<I,O>(MonoBehaviour mono,I request,Action<O> action) where O : class
    {
        mono.StartCoroutine(SendChatGPT<I, O>(request, result => action?.Invoke(result)));
    }

    private IEnumerator SendChatGPT<I, O>(I obj, Action<O> resultText) where O : class
    {
        using (var request = UnityWebRequest.Post(endpoint, "POST"))
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + api_key);
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
                var data = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
                var responsedata = JsonConvert.DeserializeObject<O>(data);
                resultText?.Invoke(responsedata);
                yield break;
            }
            else
            {
                Debug.LogError(request.error);
            }
        }
        resultText?.Invoke(null);
    }
}

public class GPTControl: baseGPTControl, IGPTControl
{
    public event Action<string> messageReceivedCallback;
    private string messages;

    public GPTControl() : base("https://api.openai.com/v1/completions") { }

    public void Init(string api_key = null, string init_text = null)
    {
        messages = baseInit(api_key, init_text) + "\n";
    }

    public void Run(MonoBehaviour mono, string text)
    {
        messages += text + "\n";
        baseRun<APIRequest, APIResponse>(mono, new APIRequest() { Prompt = messages }, result =>
        {
            var response = result.Choices.FirstOrDefault().Text;
            messageReceivedCallback?.Invoke(response);
            messages += response + "\n";
        });
    }

    #region JsonRequest
    [JsonObject]
    public class APIRequest
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

    [JsonObject]
    public class APIResponse
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

public class ChatGPTControl : baseGPTControl,IGPTControl
{
    public event Action<string> messageReceivedCallback;
    private List<Message> messages = new List<Message>();

    public ChatGPTControl() : base("https://api.openai.com/v1/chat/completions") { }

    public void Init(string api_key = null, string init_text=null)
    {
        messages.Clear();
        messages.Add(new Message() { Role = "system", Content = baseInit(api_key, init_text)});
    }

    public void Run(MonoBehaviour mono, string text)
    {
        messages.Add(new Message() { Role = "user", Content = text });
        baseRun<APIRequest, APIResponse>(mono, new APIRequest() { Messages = messages.ToArray() }, result =>
        {
            var response = result.choices.FirstOrDefault().message.Content;
            messageReceivedCallback?.Invoke(response);
            messages.Add(new Message() { Role = "assistant", Content = response });
        });
    }

    #region JsonRequest
    [JsonObject]
    public class APIRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "gpt-3.5-turbo";
        [JsonProperty("messages")]
        public Message[] Messages { get; set; } = new Message[0];
        [JsonProperty("temperature")]
        public int Temperature { get; set; } = 0;
        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; } = 500;
    }

    [JsonObject]
    public class APIResponse
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("object")]
        public string _object { get; set; }
        [JsonProperty("created")]
        public int created { get; set; }
        [JsonProperty("model")]
        public string model { get; set; }
        [JsonProperty("usage")]
        public Usage usage { get; set; }
        [JsonProperty("choices")]
        public Choice[] choices { get; set; }
    }

    [JsonObject]
    public class Usage
    {
        [JsonProperty("prompt_tokens")]
        public int prompt_tokens { get; set; }
        [JsonProperty("completion_tokens")]
        public int completion_tokens { get; set; }
        [JsonProperty("total_tokens")]
        public int total_tokens { get; set; }
    }

    [JsonObject]
    public class Choice
    {
        [JsonProperty("message")]
        public Message message { get; set; }
        [JsonProperty("finish_reason")]
        public string finish_reason { get; set; }
        [JsonProperty("index")]
        public int index { get; set; }
    }

    [JsonObject]
    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    #endregion
}
