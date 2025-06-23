using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // <- cần thêm để dùng TextMeshPro nếu bạn dùng TMP

[System.Serializable]
public class Room
{
    public int roomCode;
    public string ownerName;
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
        return currentRoom != null && currentRoom.ownerName == currentPlayerName;
    }

    public void FindRoom(string roomCode)
    {
        StartCoroutine(FindRoomCoroutine(roomCode));
    }

    private IEnumerator CreateRoomCoroutine()
    {
        string apiUrl = "http://localhost:3000/api/room/create-room";
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

            // Gán vào UI
            if (RoomCodeText != null) RoomCodeText.text = response.room.roomCode.ToString();
            if (OwnerNameText != null) OwnerNameText.text = name;

            //WebSocketClient wsClient = FindObjectOfType<WebSocketClient>();
            //if (wsClient != null)
            //{
            //    string roomCode = currentRoom.roomCode.ToString();
            //    string guestName = currentPlayerName;

            //    string message = JsonUtility.ToJson(new WebSocketJoinRoomMessage
            //    {
            //        type = "join_room",
            //        roomCode = roomCode,
            //        guestName = guestName
            //    });

            //    //wsClient.SendMessage(message);
            //}
        }
        else
        {
            Debug.LogError("Lỗi tạo phòng: " + request.error);
        }
    }

    private IEnumerator CloseRoomCoroutine()
    {
        string apiUrl = "http://localhost:3000/api/room/close-room";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đóng phòng thành công!");

            currentRoom = null;
        }
        else
        {
            Debug.LogError("Lỗi đóng phòng: " + request.error);
        }
    }

    private IEnumerator LeaveRoomCoroutine()
    {
        string apiUrl = "http://localhost:3000/api/room/leave-room";
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
            // Làm sạch thông tin phòng
            currentRoom = null;
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
        string apiUrl = "http://localhost:3000/api/room/find-room";

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

                // Cập nhật UI
                if (RoomCodeText != null) RoomCodeText.text = currentRoom.roomCode.ToString();
                if (OwnerNameText != null && response.room.ownerName != null)
                    OwnerNameText.text = response.room.ownerName;
                if (GuestNameText != null && currentPlayerName != null) 
                    GuestNameText.text = currentPlayerName;
                // Kích hoạt sự kiện tìm phòng thành công
                OnRoomJoinSuccess?.Invoke();

                //WebSocketClient wsClient = FindFirstObjectByType<WebSocketClient>();

                string roomCodeStr = currentRoom.roomCode.ToString();
                string guestName = currentPlayerName;

                string message = JsonUtility.ToJson(new WebSocketJoinRoomMessage
                {
                    type = "join_room",
                    roomCode = roomCodeStr,
                    guestName = guestName
                });

                //wsClient.SendMessage(message);

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