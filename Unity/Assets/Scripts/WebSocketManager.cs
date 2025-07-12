using UnityEngine;
using NativeWebSocket;
using TMPro;
using SimpleJSON;
using System;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private GameObject firePersonPanel;
    [SerializeField] private GameObject personFirePanel;

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetupSceneLoadHook()
    {
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "PlayPersonScene" && Instance != null)
        {
            Instance.LateBindPanels();
        }
    }

    private void LateBindPanels()
    {
        // Tìm panel theo tên GameObject
        firePersonPanel = GameObject.Find("FirePersonPanel");
        personFirePanel = GameObject.Find("PersonFirePanel");

        if (firePersonPanel == null || personFirePanel == null)
        {
            Debug.LogError("❌ Không tìm thấy panel sau khi load scene!");
            Debug.LogError($"FirePersonPanel: {(firePersonPanel != null ? "✅" : "❌")}");
            Debug.LogError($"PersonFirePanel: {(personFirePanel != null ? "✅" : "❌")}");
        }
        else
        {
            Debug.Log("✅ Gán thành công panel sau khi load scene!");

            // ✅ Ẩn cả 2 panel ngay khi vào scene (đang trong giai đoạn đặt tàu)
            SetPanelVisible(firePersonPanel, false);
            SetPanelVisible(personFirePanel, false);
        }
    }

    async void Start()
    {
        playerId = PrefsHelper.GetInt("playerId");

        websocket = new WebSocket("wss://battleship-game-production-1176.up.railway.app/");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket connected!");

            var registerMsg = new RegisterPayload(playerId);
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

                if (ListFriendInstance == null || senderId != ListFriendInstance.CurrentReceiverId)
                {
                    NotifyController.Instance?.ShowFriendNotify();
                }
            }
            else if (data["type"] == "friend_notify")
            {
                Debug.Log($"🔔 Nhận được lời mời kết bạn từ {data["fromName"]} (id={data["fromId"]})");
                NotifyController.Instance?.ShowFriendNotify();
            }
            else if (data["type"] == "room_update")
            {
                HandleRoomUpdate(data);
            }
            else if (data["type"] == "goto_place_ship")
            {
                Debug.Log("🎯 Nhận được tín hiệu từ server: chuyển sang scene đặt tàu");

                int gameId = data["gameId"].AsInt;
                int ownerId = data["ownerId"].AsInt;
                int guestId = data["guestId"].AsInt;
                int myId = PrefsHelper.GetInt("playerId");
                int opponentId = (myId == ownerId) ? guestId : ownerId;

                PrefsHelper.SetInt("gameId", gameId);
                PrefsHelper.SetInt("ownerId", ownerId);
                PrefsHelper.SetInt("guestId", guestId);
                PrefsHelper.SetInt("opponentId", opponentId);

                UnityEngine.SceneManagement.SceneManager.LoadScene("PlayPersonScene");
            }
            else if (data["type"] == "start_countdown")
            {
                Debug.Log("⏳ Nhận được tín hiệu start_countdown");
                CountdownPersonManager countdown = FindFirstObjectByType<CountdownPersonManager>();
                if (countdown != null)
                {
                    StartCoroutine(countdown.StartCountdown(() =>
                    {
                        Debug.Log("🎮 Countdown kết thúc, bắt đầu game!");
                    }));
                }
            }
            else if (data["action"] == "start_game")
            {
                Debug.Log("🎯 Nhận được tín hiệu start_game từ server");

                int gameId = data["gameId"].AsInt;
                int roomCode = data["roomCode"].AsInt;
                int ownerId = data["ownerId"].AsInt;
                int guestId = data["guestId"].AsInt;
                int myId = PrefsHelper.GetInt("playerId");
                int opponentId = (myId == ownerId) ? guestId : ownerId;

                PrefsHelper.SetInt("gameId", gameId);
                PrefsHelper.SetInt("ownerId", ownerId);
                PrefsHelper.SetInt("guestId", guestId);
                PrefsHelper.SetInt("opponentId", opponentId);

                Debug.Log($"📝 Đã lưu gameId = {gameId}, đối thủ = {opponentId}");

                UnityEngine.SceneManagement.SceneManager.LoadScene("PlayPersonScene");
            }
            else if (data["type"] == "fire_result")
            {
                Debug.Log("📨 Nhận fire_result từ đối thủ");

                FireResultPerson result = JsonUtility.FromJson<FireResultPerson>(message);
                FirePersonCellManager.Instance?.StartCoroutine(FirePersonCellManager.Instance.HandleOpponentFire(result));
            }
            else if (data["type"] == "switch_turn")
            {
                int myId = PrefsHelper.GetInt("playerId");
                int toPlayerId = data["toPlayerId"].AsInt;

                Debug.Log($"🛰 Nhận switch_turn: toPlayerId={toPlayerId}, myId={myId}");

                if (toPlayerId == myId)
                {
                    Debug.Log("🔁 Đến lượt mình!");
                    StartCoroutine(SwitchToPlayerTurn());
                }
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
                    RoomManager.Instance.SetGuestId(data["playerId"].AsInt);
                }
                break;
            case "leave":
                if (RoomManager.Instance != null && role == "guest")
                {
                    RoomManager.Instance.SetGuestName("...");
                    RoomManager.Instance.SetGuestId(0);
                }
                break;
            case "closed":
                Debug.Log("❌ Phòng đã bị đóng, quay lại scene chính");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
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

    public void SendRawJson(string json)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            Debug.Log("📤 Gửi JSON WebSocket: " + json);
            websocket.SendText(json);
        }
        else
        {
            Debug.LogWarning("⚠️ WebSocket chưa kết nối.");
        }
    }

    private IEnumerator SendMessageToApi(int receiverId, string content)
    {
        string sendUrl = "https://battleship-game-production-1176.up.railway.app/api/message/send";
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

    public void SendRoomClosedEvent(int roomCode, int ownerId, int guestId)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ WebSocket chưa sẵn sàng gửi close_room event.");
            return;
        }

        string json = $"{{" +
            $"\"action\":\"close_room\"," +
            $"\"roomCode\":{roomCode}," +
            $"\"ownerId\":{ownerId}," +
            $"\"guestId\":{guestId}" +
            $"}}";

        Debug.Log($"📤 Gửi close_room WebSocket: {json}");
        websocket.SendText(json);
    }

    public void SendFireResult(int cellX, int cellY, string result, int shipId, bool isWin)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("⚠️ WebSocket chưa kết nối để gửi fire_result.");
            return;
        }
        int gameId = PrefsHelper.GetInt("gameId");
        int opponentId = PrefsHelper.GetInt("opponentId");

        string json = $"{{" +
            $"\"action\":\"fire_result\"," +
            $"\"gameId\":{gameId}," +
            $"\"shooterId\":{playerId}," +
            $"\"opponentId\":{opponentId}," +
            $"\"cellX\":{cellX}," +
            $"\"cellY\":{cellY}," +
            $"\"result\":\"{result}\"," +
            $"\"shipId\":{shipId}," +
            $"\"isWin\":{isWin.ToString().ToLower()}" +
            $"}}";

        Debug.Log("📤 Gửi fire_result WebSocket: " + json);
        websocket.SendText(json);
    }

    private string GetRole()
    {
        return PrefsHelper.GetString("isHost") == "true" ? "host" : "guest";
    }

    /// <summary>
    /// ✅ Hàm mới: Chuyển sang lượt người chơi khi nhận switch_turn
    /// </summary>
    private IEnumerator SwitchToPlayerTurn()
    {
        // Kiểm tra xem còn đang đặt tàu không
        GameObject placeShipPanel = GameObject.Find("PlaceShipPanel");
        if (placeShipPanel != null && placeShipPanel.activeInHierarchy)
        {
            Debug.LogWarning("⛔ Đang đặt tàu (PlaceShipPanel active), chưa được chuyển turn.");
            yield break;
        }

        // Đợi panel được gán
        float waitTime = 0f;
        while ((firePersonPanel == null || personFirePanel == null) && waitTime < 5f)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (firePersonPanel == null || personFirePanel == null)
        {
            Debug.LogError("❌ Panel vẫn null sau khi đợi!");
            yield break;
        }

        // Đợi FirePersonCellManager được khởi tạo
        waitTime = 0f;
        while (FirePersonCellManager.Instance == null && waitTime < 10f)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (FirePersonCellManager.Instance == null)
        {
            Debug.LogError("❌ FirePersonCellManager vẫn null sau timeout!");
            yield break;
        }

        // Chuyển lượt
        FirePersonCellManager.isPlayerTurn = true;

        // Hiển thị panel change turn
        FirePersonCellManager.Instance.StartCoroutine(
            FirePersonCellManager.Instance.ShowChangeTurnPanel());

        // Cập nhật panel visibility
        FirePersonCellManager.Instance.UpdatePanelVisibility();
    }

    /// <summary>
    /// ✅ Hàm cải tiến: Hiển thị/ẩn panel bằng CanvasGroup
    /// </summary>
    public void SetPanelVisible(GameObject panel, bool visible)
    {
        if (panel == null)
        {
            Debug.LogWarning($"⚠️ Panel null khi SetPanelVisible({visible})");
            return;
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;

        Debug.Log($"[SetPanelVisible] {panel.name} → {(visible ? "Visible" : "Hidden")}");
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
    public string type;
    public int player_Id;

    public RegisterPayload(int id)
    {
        type = "register";
        player_Id = id;
    }
}