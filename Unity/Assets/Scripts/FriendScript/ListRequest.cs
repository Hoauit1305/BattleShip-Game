using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using TMPro;

public class ListRequest : MonoBehaviour
{
    public GameObject requestItemPrefab; // Prefab dòng bạn bè (có Text + Button)
    public Transform contentPanel;      // Nơi chứa các dòng bạn bè
    public ListFriend listFriendComponent;
    public string apiUrl = "http://localhost:3000/api/friend/pending"; // Thay đổi nếu cần
    private string token; // Gán từ nơi bạn lưu token sau khi đăng nhập

    public void Refresh()
    {
        StartCoroutine(GetListRequest());
    }
    void OnEnable()
    {
        token = PrefsHelper.GetString("token");
        StartCoroutine(GetListRequest());
    }

    IEnumerator GetListRequest()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi khi lấy yêu cầu: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            JSONNode data = JSON.Parse(json);
            Debug.Log("Request JSON: " + json);

            // Xóa các dòng cũ
            foreach (Transform child in contentPanel)
            {
                Destroy(child.gameObject);
            }

            foreach (JSONNode friend in data.AsArray)
            {
                GameObject newFriendItem = Instantiate(requestItemPrefab, contentPanel);
                string requesterId = friend["Requester_Id"];
                newFriendItem.transform.Find("RequestIdText").GetComponent<TMP_Text>().text = "id: " + requesterId;
                newFriendItem.transform.Find("NameText").GetComponent<TMP_Text>().text = friend["Name"];

                Button AcceptBtn = newFriendItem.transform.Find("AcceptButton").GetComponent<Button>();
                Button RejectBtn = newFriendItem.transform.Find("RejectButton").GetComponent<Button>();
                Debug.Log("Found AcceptBtn: " + AcceptBtn);
                Debug.Log("Found RejectBtn: " + RejectBtn);
                AcceptBtn.onClick.AddListener(() =>
                {
                    Debug.Log("👉 Nút Accept được nhấn cho ID: " + requesterId);
                    StartCoroutine(HandleFriendRequest("accept", requesterId, newFriendItem));
                });
                RejectBtn.onClick.AddListener(() =>
                {
                    Debug.Log("👉 Nút Reject được nhấn cho ID: " + requesterId);
                    StartCoroutine(HandleFriendRequest("reject", requesterId, newFriendItem));
                });
               
            }
        }
    }
    IEnumerator HandleFriendRequest(string action, string requesterId, GameObject itemToRemove)
    {
        string url = $"http://localhost:3000/api/friend/{action}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        string jsonBody = $"{{\"requesterId\": \"{requesterId}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Lỗi khi gửi {action}: {request.error}");
        }
        else
        {
            Debug.Log($"✅ {action} thành công với ID: {requesterId}");
            Destroy(itemToRemove); // Xóa dòng yêu cầu đã xử lý
            // 👇 Reset UI sau khi thao tác thành công
            StartCoroutine(GetListRequest());
            if (listFriendComponent != null)
            {
                listFriendComponent.Refresh();
            }

        }
    }
}
