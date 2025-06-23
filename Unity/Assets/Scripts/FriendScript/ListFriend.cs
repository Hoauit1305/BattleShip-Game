    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using System.Collections;
    using System.Collections.Generic;
    using SimpleJSON;
    using TMPro;
public class ListFriend : MonoBehaviour
{
    public GameObject friendItemPrefab; // Prefab dòng bạn bè (có Text + Button)
    public Transform contentPanel;      // Nơi chứa các dòng bạn bè
    public string apiUrl = "http://localhost:3000/api/friend/list"; // Thay đổi nếu cần
    private string token; // Gán từ nơi bạn lưu token sau khi đăng nhập

    public GameObject chatPanel;
    public TMP_Text receiverNameText;
    public Transform messageContent;
    public GameObject messageItemPrefab; // prefab tin nhắn (đã nói ở trên)
    public TMP_InputField inputField;
    public Button sendButton;
    private int currentReceiverId;
    public int CurrentReceiverId => currentReceiverId;

    private string currentReceiverName;
    public void Refresh()
    {
        StartCoroutine(GetFriendList());
    }
    void OnEnable()
    {
        token = PrefsHelper.GetString("token");
        StartCoroutine(GetFriendList());
    }

    IEnumerator GetFriendList()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi khi lấy danh sách bạn: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            JSONNode data = JSON.Parse(json);
            Debug.Log("Friend JSON: " + json);
            // Xóa các dòng cũ
            foreach (Transform child in contentPanel)
            {
                Destroy(child.gameObject);
            }

            foreach (JSONNode friend in data.AsArray)
            {
                GameObject newFriendItem = Instantiate(friendItemPrefab, contentPanel);
                newFriendItem.transform.Find("NameText").GetComponent<TMP_Text>().text = friend["Name"];
                newFriendItem.transform.Find("StatusText").GetComponent<TMP_Text>().text = friend["Status"];
                newFriendItem.transform.Find("StatusText").GetComponent<TMP_Text>().color =
                    friend["Status"] == "online" ? Color.green : Color.red;
                Button chatBtn = newFriendItem.transform.Find("ChatButton").GetComponent<Button>();
                chatBtn.onClick.AddListener(() =>
                {
                    Debug.Log("Nhắn tin với " + friend["Name"]);
                    currentReceiverId = friend["Player_Id"].AsInt;
                    currentReceiverName = friend["Name"];
                    receiverNameText.text = currentReceiverName;
                    chatPanel.SetActive(true);
                    StartCoroutine(LoadChatHistory(currentReceiverId));
                    sendButton.onClick.RemoveAllListeners();
                    sendButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(SendMessage(currentReceiverId, inputField.text));
                    });
                });
                // Có thể gán thêm sự kiện nút "Nhắn Tin" tại đây
                Debug.Log($"Friend: name={friend["Name"]}, status={friend["Status"]}");

            }
        }
    }
    public IEnumerator LoadChatHistory(int receiverId)
    {
        string historyUrl = $"http://localhost:3000/api/message/history/{receiverId}";
        UnityWebRequest request = UnityWebRequest.Get(historyUrl);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi lấy lịch sử chat: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            JSONNode messages = JSON.Parse(json);

            // Xoá tin nhắn cũ
            foreach (Transform child in messageContent)
                Destroy(child.gameObject);

            foreach (JSONNode msg in messages.AsArray)
            {
                GameObject msgItem = Instantiate(messageItemPrefab, messageContent);
                TMP_Text msgText = msgItem.transform.Find("ContentText").GetComponent<TMP_Text>();
                msgText.text = msg["Content"];
                yield return new WaitForEndOfFrame();
                bool isSender = msg["Sender_Id"].AsInt == PrefsHelper.GetInt("playerId"); // bạn cần lưu userId
                msgText.alignment = isSender ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            }
            // Đợi 1 frame để layout group xử lý
            yield return null;

            // Cập nhật lại layout để kích hoạt Scroll
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent.GetComponent<RectTransform>());
            ScrollRect scrollRect = messageContent.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    public class MessageData
    {
        public int receiverId;
        public string content;
    }
    IEnumerator SendMessage(int receiverId, string content)
    {
        if (string.IsNullOrEmpty(content)) yield break;
        string sendUrl = "http://localhost:3000/api/message/send";
        MessageData data = new MessageData { receiverId = receiverId, content = content };
        string jsonString = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);

        UnityWebRequest request = new UnityWebRequest(sendUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        Debug.Log("Sending: " + jsonString);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gửi tin nhắn thất bại: " + request.error + "\n" + request.downloadHandler.text);
        }
        else
        {
            inputField.text = "";
            StartCoroutine(LoadChatHistory(receiverId));
        }
    }
}