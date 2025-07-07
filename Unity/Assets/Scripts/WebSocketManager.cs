using UnityEngine;
using NativeWebSocket;
using TMPro;
using SimpleJSON;
using System;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class WebSocketManager : MonoBehaviour
{
    private WebSocket websocket;
    public static WebSocketManager Instance;

    private int playerId;

    private ListFriend _listFriend;
    private ListFriend ListFriendInstance
    {
        get
        {
            if (_listFriend == null)
            {
                _listFriend = FindFirstObjectByType<ListFriend>();
                if (_listFriend == null)
                    Debug.LogWarning("❌ Không tìm thấy ListFriend trong scene!");
                else
                    Debug.Log("✅ Tìm thấy ListFriend tự động.");
            }
            return _listFriend;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        playerId = PrefsHelper.GetInt("playerId");
    
        websocket = new WebSocket("https://battleship-game-production.up.railway.app/");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket connected!");

            var registerMsg = new RegisterPayload
            {
                player_Id = playerId
            };

            string json = JsonUtility.ToJson(registerMsg);
            Debug.Log("📤 Gửi đăng ký WebSocket: " + json);
            websocket.SendText(json);

        };

        websocket.OnError += (e) =>
        {
            Debug.Log("❌ WebSocket error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 WebSocket closed");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 Message from server: " + message);

            JSONNode data = JSON.Parse(message);

            if (data["type"] == "new_message")
            {
                int senderId = data["data"]["senderId"].AsInt;
                string content = data["data"]["content"];
                Debug.Log($"💬 Tin nhắn từ {senderId}: {content}");

                StartCoroutine(HandleIncomingMessage(senderId));
            }
        };

        await websocket.Connect();
    }

    IEnumerator HandleIncomingMessage(int senderId)
    {
        while (ListFriendInstance == null)
        {
            Debug.LogWarning("⏳ Đợi ListFriendInstance được gán...");
            yield return null;
        }

        if (senderId == ListFriendInstance.CurrentReceiverId)
        {
            ListFriendInstance.StartCoroutine(ListFriendInstance.LoadChatHistory(senderId));
        }
        else
        {
            Debug.Log("📥 Nhận tin nhắn từ người không phải đang chat.");
        }
    }

    void Update()
    {
        if (websocket != null)
            websocket.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
            await websocket.Close();
    }

    public void SendMessage(int receiverId, string content)
    {
        StartCoroutine(SendMessageCoroutine(receiverId, content));
        SendMessageViaWebSocket(receiverId, content);
    }

    private IEnumerator SendMessageCoroutine(int receiverId, string content)
    {
        yield return StartCoroutine(SendMessageToApi(receiverId, content));

        if (ListFriendInstance != null)
        {
            ListFriendInstance.StartCoroutine(ListFriendInstance.LoadChatHistory(receiverId));
        }
        else
        {
            Debug.LogWarning("⚠️ ListFriendInstance is null sau khi gửi tin nhắn!");
        }
    }

    private async void SendMessageViaWebSocket(int receiverId, string content)
    {
        if (websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ WebSocket chưa kết nối!");
            return;
        }

        if (string.IsNullOrWhiteSpace(content)) return;

        //var payload = new Dictionary<string, object>
        //{
        //    { "action", "send_message" },
        //    { "senderId", playerId },
        //    { "receiverId", receiverId },
        //    { "content", content }
        //};

        string jsonString = $"{{\"action\":\"send_message\",\"senderId\":{playerId},\"receiverId\":{receiverId},\"content\":\"{content}\"}}";

        Debug.Log("📤 Sending WebSocket message: " + jsonString);
        await websocket.SendText(jsonString);
    }

    private IEnumerator SendMessageToApi(int receiverId, string content)
    {
        string sendUrl = "http://localhost:3000/api/message/send";
        var data = new ListFriend.MessageData { receiverId = receiverId, content = content };
        string jsonString = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);

        UnityWebRequest request = new UnityWebRequest(sendUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PrefsHelper.GetString("token"));

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Gửi tin nhắn thất bại: " + request.error);
        }
        else
        {
            Debug.Log("✅ Tin nhắn đã lưu vào DB");
        }
    }
}

[Serializable]
public class OutgoingMessage
{
    public string action = "send_message";
    public int senderId;
    public int receiverId;
    public string content;
}
[Serializable]
public class RegisterPayload
{
    public string type = "register";
    public int player_Id;
}

