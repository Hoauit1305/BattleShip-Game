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
    
        websocket = new WebSocket("wss://battleship-game-production-1176.up.railway.app/");

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
            else if (data["type"] == "room_update")
            {
                HandleRoomUpdate(data);
            }
        };

        await websocket.Connect();
    }
    void HandleRoomUpdate(JSONNode data)
    {
        string action = data["action"];
        string playerName = data["playerName"];
        string role = data["role"];
        int roomCode = data["roomCode"].AsInt;
        Debug.Log($"📡 Room update: {action} - {playerName} - role: {role}");

        switch (action)
        {
            case "join":
                if (RoomManager.Instance != null && role == "guest")
                {
                    RoomManager.Instance.SetGuestName(playerName);
                }
                break;

            case "leave":
                if (RoomManager.Instance != null && role == "guest")
                {
                    RoomManager.Instance.SetGuestName("Mời"); // Xoá tên guest
                }
                break;

            case "closed":
                Debug.Log("❌ Phòng đã bị đóng, quay lại scene chính");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                break;
        }
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

        string jsonString = $"{{\"action\":\"send_message\",\"senderId\":{playerId},\"receiverId\":{receiverId},\"content\":\"{content}\"}}";

        Debug.Log("📤 Sending WebSocket message: " + jsonString);
        await websocket.SendText(jsonString);
    }

    private IEnumerator SendMessageToApi(int receiverId, string content)
    {
        string sendUrl = "https://battleship-game-production.up.railway.app/api/message/send";
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
    public void SendRoomEvent(string action, int roomCode, int targetPlayerId = -1)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ WebSocket chưa sẵn sàng gửi room event.");
            return;
        }

        string playerName = PrefsHelper.GetString("name");

        // Tạo payload JSON thủ công
        string json = $"{{" +
            $"\"action\":\"{action}\"," +
            $"\"roomCode\":{roomCode}," +
            $"\"playerId\":{playerId}," +
            $"\"playerName\":\"{playerName}\"," +
            $"\"role\":\"{GetRole()}\"," +
            $"\"targetId\":{targetPlayerId}" +
            $"}}";

        Debug.Log($"📤 Sending room event: {json}");
        websocket.SendText(json);
    }

    private string GetRole()
    {
        // Tùy bạn xác định role từ scene hoặc Prefs
        return PrefsHelper.GetString("isHost") == "true" ? "host" : "guest";
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

