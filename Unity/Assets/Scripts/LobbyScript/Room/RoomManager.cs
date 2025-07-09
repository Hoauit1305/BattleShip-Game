using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // <- cần thêm để dùng TextMeshPro nếu bạn dùng TMP
using UnityEngine.SceneManagement;
[System.Serializable]
public class Room
{
    public int roomCode;
    public int ownerId;
    public string ownerName;
    public int guestId;
    public string guestName;
}

[System.Serializable]
public class CreateRoomResponse
{
    public string message;
    public Room room;
}

[System.Serializable]
public class GenericResponse
{
    public string message;
    public bool success;
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Room UI")]
    public TMP_Text RoomCodeText;
    public TMP_Text OwnerNameText;
    public TMP_Text GuestNameText;

    private Room currentRoom;
    private string currentPlayerName;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (!string.IsNullOrEmpty(PrefsHelper.GetString("name")))
        {
            currentPlayerName = PrefsHelper.GetString("name");
        }
    }

    public void CreateRoom()
    {
        StartCoroutine(CreateRoomCoroutine());
    }

    public void CloseRoom()
    {
        StartCoroutine(CloseRoomCoroutine());
    }

    public void LeaveRoom()
    {
        StartCoroutine(LeaveRoomCoroutine());
    }

    public bool IsRoomOwner()
    {
        // Kiểm tra xem người chơi hiện tại có phải là chủ phòng không
        Debug.Log("currentRoom: " + currentRoom);
        Debug.Log("ownerName: " + currentRoom.ownerName);
        Debug.Log("currentPlayerName: " + currentPlayerName);
        return currentRoom != null && currentRoom.ownerName == currentPlayerName;
    }

    public void FindRoom(string roomCode)
    {
        StartCoroutine(FindRoomCoroutine(roomCode));
    }

    public void StartPersonGame()
    {
        StartCoroutine(CreateGameIdForPersonCoroutine());
    }

    private IEnumerator CreateRoomCoroutine()
    {
        string apiUrl = "https://battleship-game-production-1176.up.railway.app/api/room/create-room";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();

        string token = PrefsHelper.GetString("token");
        string name = PrefsHelper.GetString("name");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Tạo phòng thành công: " + request.downloadHandler.text);

            // Parse JSON
            CreateRoomResponse response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);

            currentRoom = response.room;
            PrefsHelper.SetInt("ownerId", currentRoom.ownerId);

            // Gán vào UI
            if (RoomCodeText != null) RoomCodeText.text = response.room.roomCode.ToString();
            if (OwnerNameText != null) OwnerNameText.text = name;

            // Sau khi tạo phòng thành công
            PrefsHelper.SetString("isHost", "true"); // Xác định vai trò
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.SendRoomEvent("join_room", currentRoom.roomCode);
            }
        }
        else
        {
            Debug.LogError("Lỗi tạo phòng: " + request.error);
        }
    }

    private IEnumerator CloseRoomCoroutine()
    {
        string apiUrl = "https://battleship-game-production-1176.up.railway.app/api/room/close-room";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đóng phòng thành công!");

            if (WebSocketManager.Instance != null && currentRoom != null)
            {
                WebSocketManager.Instance.SendRoomClosedEvent(currentRoom.roomCode, currentRoom.ownerId, currentRoom.guestId);
            }

            currentRoom = null;

            PrefsHelper.DeleteKey("ownerId");
            PrefsHelper.DeleteKey("guestId");
            PrefsHelper.DeleteKey("isHost");
        }
        else
        {
            Debug.LogError("Lỗi đóng phòng: " + request.error);
        }
    }

    private IEnumerator LeaveRoomCoroutine()
    {
        string apiUrl = "https://battleship-game-production-1176.up.railway.app/api/room/leave-room";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Rời phòng thành công!");
            // Phân tích kết quả nếu cần
            GenericResponse response = JsonUtility.FromJson<GenericResponse>(request.downloadHandler.text);

            if (WebSocketManager.Instance != null && currentRoom != null)
            {
                WebSocketManager.Instance.SendRoomEvent("leave_room", currentRoom.roomCode, currentRoom.ownerId);
            }

            currentRoom = null;

            PrefsHelper.DeleteKey("ownerId");
            PrefsHelper.DeleteKey("guestId");
            PrefsHelper.DeleteKey("isHost");
        }
        else
        {
            Debug.LogError("Lỗi rời phòng: " + request.error);
        }
    }
    public delegate void RoomEvent();
    public event RoomEvent OnRoomJoinSuccess;
    private IEnumerator FindRoomCoroutine(string roomCode)
    {
        string apiUrl = "https://battleship-game-production-1176.up.railway.app/api/room/find-room";

        // Chuyển đổi roomCode từ string sang int
        if (!int.TryParse(roomCode, out int roomCodeInt))
        {
            Debug.LogError("Mã phòng không hợp lệ. Vui lòng nhập số.");
            // Hiển thị thông báo lỗi cho người dùng nếu cần
            yield break;
        }

        // Tạo object request có thể serialize
        RoomCodeRequest requestData = new RoomCodeRequest { roomCode = roomCodeInt };

        // Tạo body JSON
        string jsonBody = JsonUtility.ToJson(requestData);
        Debug.Log("JSON body: " + jsonBody);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Tham gia phòng thành công: " + request.downloadHandler.text);
            try
            {
                CreateRoomResponse response = JsonUtility.FromJson<CreateRoomResponse>(request.downloadHandler.text);
                currentRoom = response.room;
                PrefsHelper.SetInt("ownerId", currentRoom.ownerId);

                // Cập nhật UI
                if (RoomCodeText != null) RoomCodeText.text = currentRoom.roomCode.ToString();
                if (OwnerNameText != null && response.room.ownerName != null)
                    OwnerNameText.text = response.room.ownerName;
                if (GuestNameText != null && currentPlayerName != null) 
                    GuestNameText.text = currentPlayerName;
                // Kích hoạt sự kiện tìm phòng thành công
                OnRoomJoinSuccess?.Invoke();

                string roomCodeStr = currentRoom.roomCode.ToString();
                string guestName = currentPlayerName;

                string message = JsonUtility.ToJson(new WebSocketJoinRoomMessage
                {
                    type = "join_room",
                    roomCode = roomCodeStr,
                    guestName = guestName
                });

                PrefsHelper.SetString("isHost", "false"); // Xác định vai trò

                if (WebSocketManager.Instance != null)
                {
                    WebSocketManager.Instance.SendRoomEvent("join_room", currentRoom.roomCode, currentRoom.ownerId);

                }


            }
            catch (System.Exception e)
            {
                Debug.LogError("Lỗi xử lý response: " + e.Message);
                Debug.Log("Response data: " + request.downloadHandler.text);
            }
        }
        else
        {
            Debug.LogError("Lỗi tham gia phòng: " + request.error + " - " + request.downloadHandler.text);
        }
    }
    public void SetGuestName(string guestName)
    {
        if (GuestNameText != null)
        {
            GuestNameText.text = guestName;
        }

        if (currentRoom != null)
        {
            currentRoom.guestName = guestName;
        }
    }

    public void SetGuestId(int id)
    {
        if (currentRoom != null)
        {
            currentRoom.guestId = id;
            if (id > 0)
            {
                PrefsHelper.SetInt("guestId", id);
                Debug.Log($"✅ Set guestId = {id}");
            }
            else
            {
                PrefsHelper.DeleteKey("guestId");
                Debug.Log("🗑 Xoá guestId");
            }
        }
    }

    private IEnumerator CreateGameIdForPersonCoroutine()
    {
        string token = PrefsHelper.GetString("token");
        int ownerId = PrefsHelper.GetInt("ownerId");
        int guestId = PrefsHelper.GetInt("guestId");
        if (string.IsNullOrEmpty(token) || ownerId == 0 || guestId == 0)
        {
            Debug.LogError("Thiếu token hoặc ownerId/guestId");
            yield break;
        }

        // Gửi request tạo gameId giữa 2 người
        var requestBody = new PlayerPairRequest { playerId1 = ownerId, playerId2 = guestId };
        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production-1176.up.railway.app/api/gameplay/create-gameid-fire-person", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            GameIDResponse response = JsonUtility.FromJson<GameIDResponse>(request.downloadHandler.text);
            PrefsHelper.SetInt("gameId", response.gameId);
            Debug.Log($"✅ Đã tạo gameId: {response.gameId}");

            // Chuyển đến scene chiến đấu người
            SceneManager.LoadScene("PlayPersonScene");
        }
        else
        {
            Debug.LogError($"❌ Lỗi tạo gameId cho người: {request.error} - {request.downloadHandler.text}");
        }
    }

    [System.Serializable]
    public class PlayerPairRequest
    {
        public int playerId1;
        public int playerId2;
    }

    [System.Serializable]
    public class GameIDResponse
    {
        public string message;
        public int gameId;
    }

    [System.Serializable]
    public class RoomCodeRequest
    {
        public int roomCode;
    }

    [System.Serializable]
    public class WebSocketJoinRoomMessage
    {
        public string type;
        public string roomCode;
        public string guestName;
    }


}