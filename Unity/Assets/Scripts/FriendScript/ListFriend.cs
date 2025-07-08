using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using TMPro;

public class ListFriend : MonoBehaviour
{
    public GameObject friendItemPrefab;
    public Transform contentPanel;
    private string token;

    public GameObject chatPanel;
    public TMP_Text receiverNameText;
    public Transform messageContent;
    public GameObject messageItemPrefab;
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
        string apiUrl = "https://battleship-game-production-1176.up.railway.app/api/friend/list";
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(new byte[0]); // Nếu không cần gửi body
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Lỗi khi lấy danh sách bạn: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            JSONNode data = JSON.Parse(json);

            // Clear old list
            foreach (Transform child in contentPanel)
                Destroy(child.gameObject);

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
                    currentReceiverId = friend["Player_Id"].AsInt;
                    currentReceiverName = friend["Name"];
                    receiverNameText.text = currentReceiverName;
                    chatPanel.SetActive(true);

                    StartCoroutine(LoadChatHistory(currentReceiverId));

                    sendButton.onClick.RemoveAllListeners();
                    sendButton.onClick.AddListener(() =>
                    {
                        string content = inputField.text;
                        if (!string.IsNullOrEmpty(content))
                        {
                            var ws = WebSocketManager.Instance;

                            if (ws != null)
                            {
                                inputField.text = "";

                                // ✅ Gửi và tự động cập nhật chat sau khi lưu DB
                                ws.SendMessage(currentReceiverId, content);
                            }
                            else
                            {
                                Debug.LogError("❌ WebSocketManager.Instance chưa khởi tạo!");
                            }
                        }
                    });
                });

                Debug.Log($"📘 Friend: name={friend["Name"]}, status={friend["Status"]}");
            }
        }
    }

    public IEnumerator LoadChatHistory(int receiverId)
    {
        string historyUrl = $"https://battleship-game-production-1176.up.railway.app/api/message/history/{receiverId}";
        UnityWebRequest request = new UnityWebRequest(historyUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(new byte[0]); // Nếu không cần gửi body
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Lỗi lấy lịch sử chat: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            JSONNode messages = JSON.Parse(json);

            // Clear old messages
            foreach (Transform child in messageContent)
                Destroy(child.gameObject);

            foreach (JSONNode msg in messages.AsArray)
            {
                GameObject msgItem = Instantiate(messageItemPrefab, messageContent);
                TMP_Text msgText = msgItem.transform.Find("ContentText").GetComponent<TMP_Text>();
                msgText.text = msg["Content"];
                yield return new WaitForEndOfFrame();

                bool isSender = msg["Sender_Id"].AsInt == PrefsHelper.GetInt("playerId");
                msgText.alignment = isSender ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            }

            // Force update layout
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent.GetComponent<RectTransform>());
            ScrollRect scrollRect = messageContent.GetComponentInParent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    [System.Serializable]
    public class MessageData
    {
        public int receiverId;
        public string content;
    }
    void OnDisable()
    {
        sendButton?.onClick.RemoveAllListeners();
    }
}
