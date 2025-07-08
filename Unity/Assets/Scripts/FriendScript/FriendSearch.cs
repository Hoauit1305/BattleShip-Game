using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using System.Collections;

public class FriendSearch : MonoBehaviour
{
    public TMP_InputField searchInputField;
    public Button searchButton;
    public GameObject playerItemPrefab;
    public Transform contentPanel;
    public string apiBaseUrl = "https://battleship-game-production-1176.up.railway.app/api/friend/search/";

    private string token;
    private string myPlayerId;

    void Start()
    {
        token = PrefsHelper.GetString("token");
        myPlayerId = PrefsHelper.GetString("playerId"); // cần lưu khi login

        searchButton.onClick.AddListener(OnSearchButtonClicked);
    }

    void OnSearchButtonClicked()
    {
        string playerId = searchInputField.text.Trim();

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("Nhập PlayerId trước khi tìm!");
            return;
        }

        StartCoroutine(SearchPlayer(playerId));
    }

    IEnumerator SearchPlayer(string playerId)
    {
        string url = apiBaseUrl + playerId;
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(new byte[0]); // Nếu không cần gửi body
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        // Xóa các dòng cũ trong ScrollView
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Instantiate prefab
        GameObject newItem = Instantiate(playerItemPrefab, contentPanel);

        TMP_Text nameText = newItem.transform.Find("NameText").GetComponent<TMP_Text>();
        TMP_Text statusText = newItem.transform.Find("StatusText").GetComponent<TMP_Text>();
        TMP_Text messageText = newItem.transform.Find("MessageText").GetComponent<TMP_Text>();
        Button sendButton = newItem.transform.Find("AddFrButton").GetComponent<Button>(); // tên "Button", nếu khác sửa tên đúng

        // Nếu không tìm thấy player (404) hoặc tìm bản thân (400)
        if (request.responseCode == 404)
        {
            Debug.Log("Không tìm thấy player! (404)");

            nameText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
            sendButton.gameObject.SetActive(false);
            messageText.gameObject.SetActive(true);
            messageText.text = "Không tìm thấy player!";
            yield break;
        }
        else if (request.responseCode == 400)
        {
            Debug.Log("Bạn đang tìm chính mình! (400)");

            nameText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
            sendButton.gameObject.SetActive(false);
            messageText.gameObject.SetActive(true);
            messageText.text = "Đây là tài khoản của bạn!";
            yield break;
        }


        // Lỗi khác
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi khi tìm player: " + request.error);
            yield break;
        }

        // Nếu thành công
        string json = request.downloadHandler.text;
        Debug.Log("SearchPlayer JSON: " + json);

        JSONNode data = JSON.Parse(json);

        string foundPlayerId = data["Player_Id"];
        string foundName = data["Name"];
        string friendStatus = data["FriendStatus"];
        string playerStatus = data["PlayerStatus"];

        string myPlayerId = PrefsHelper.GetString("playerId");

        // Kiểm tra bản thân
        if (foundPlayerId == myPlayerId)
        {
            nameText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
            sendButton.gameObject.SetActive(false);
            messageText.gameObject.SetActive(true);
            messageText.text = "Đây là tài khoản của bạn!";
            yield break;
        }

        // Kiểm tra đã là bạn bè
        if (friendStatus == "friend" || friendStatus == "accepted")
        {
            nameText.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);
            sendButton.gameObject.SetActive(false);
            messageText.gameObject.SetActive(true);
            messageText.text = "Đã là bạn bè!";
            yield break;
        }

        // Nếu hợp lệ (chưa là bạn, ko phải bản thân)
        nameText.gameObject.SetActive(true);
        statusText.gameObject.SetActive(true);
        sendButton.gameObject.SetActive(true);
        messageText.gameObject.SetActive(false);

        nameText.text = foundName;
        statusText.text = playerStatus;
        statusText.color = playerStatus == "online" ? Color.green : Color.red;

        // Xóa listener cũ để tránh bị double
        sendButton.onClick.RemoveAllListeners();

        sendButton.onClick.AddListener(() =>
        {
            Debug.Log("Gửi lời mời tới playerId: " + playerId);
            StartCoroutine(SendFriendRequestCoroutine(playerId, newItem));
        });
    }
    IEnumerator SendFriendRequestCoroutine(string addresseeId, GameObject newItem)
    {
        string url = "https://battleship-game-production-1176.up.railway.app/api/friend/request";

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        string jsonBody = "{\"addresseeId\": \"" + addresseeId + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        TMP_Text messageText = newItem.transform.Find("MessageText").GetComponent<TMP_Text>();
        Button sendButton = newItem.transform.Find("AddFrButton").GetComponent<Button>();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi khi gửi lời mời: " + request.error);
            messageText.gameObject.SetActive(true);
            messageText.text = "Lỗi khi gửi lời mời!";
        }
        else
        {
            Debug.Log("Đã gửi lời mời kết bạn thành công!");

            // Hiện messageText
            messageText.gameObject.SetActive(true);
            messageText.text = "Đã gửi yêu cầu kết bạn thành công!";

            // Ẩn nút Add
            sendButton.gameObject.SetActive(false);
        }
    }
}
