using UnityEngine;
using NativeWebSocket;
using System.Text;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;

    async void Start()
    {
        websocket = new WebSocket("ws://localhost:3000");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket opened!");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("❌ WebSocket error: " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 WebSocket closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("📩 Message from server: " + message);

            try
            {
                var response = JsonUtility.FromJson<RoomUpdatedMessage>(message);
                if (response.type == "room_updated")
                {
                    Debug.Log("👥 Khách mời: " + response.guestName);
                    RoomManager.Instance?.SetGuestName(response.guestName);
                }
            }
            catch
            {
                Debug.LogWarning("⚠️ Không thể parse JSON từ server.");
            }
        };

        await websocket.Connect();
    }

    public async void SendMessage(string msg)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(msg);
            Debug.Log("📤 Sent: " + msg);
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    async void OnApplicationQuit()
    {
        await websocket.Close();
    }
    [System.Serializable]
    public class RoomUpdatedMessage
    {
        public string type;
        public string roomCode;
        public string message;
        public string guestName;
    }

}
