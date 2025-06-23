using UnityEngine;
using NativeWebSocket;
using TMPro;
using SimpleJSON;
using System;
using System.Text;
using System.Collections;

public class WebSocketManager : MonoBehaviour
{
    private WebSocket websocket;
    public static WebSocketManager Instance;

    public ListFriend listFriend; // Kéo vào từ Inspector

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
        string token = PrefsHelper.GetString("token");
        int playerId = PrefsHelper.GetInt("playerId");

        websocket = new WebSocket($"ws://localhost:3000?token={token}");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connected!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("WebSocket error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket closed");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Received message from server: " + message);

            JSONNode data = JSON.Parse(message);

            if (data["event"] == "new_message")
            {
                int senderId = data["data"]["senderId"].AsInt;
                if (senderId == listFriend.CurrentReceiverId)
                {
                    // Nếu đang chat với người gửi thì load lại lịch sử
                    listFriend.StartCoroutine(listFriend.LoadChatHistory(senderId));
                }
                else
                {
                    Debug.Log("Bạn nhận được tin nhắn từ người khác, chưa hiển thị.");
                    // Có thể hiển thị thông báo nhỏ
                }
            }
        };

        await websocket.Connect();
    }

    async void Update()
    {
        if (websocket != null)
            websocket.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
