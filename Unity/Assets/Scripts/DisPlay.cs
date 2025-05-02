using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class DisPlay : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text idText;

    private void OnEnable()
    {
        StartCoroutine(GetDisplayInfo());
    }

    IEnumerator GetDisplayInfo()
    {
        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/display/user", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Thêm Authorization token
        string token = PlayerPrefs.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Dữ liệu nhận: " + request.downloadHandler.text);
            DisplayResponse response = JsonUtility.FromJson<DisplayResponse>(request.downloadHandler.text);
            nameText.text = response.name;  // Chỉnh lại 'name' thay vì 'username'
            idText.text = "ID: " + response.id;  // Chỉnh lại 'id' thay vì 'playerId'
        }
        else
        {
            Debug.LogError("Lỗi API: " + request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class DisplayResponse
{
    public string name;  // Đảm bảo 'name' được sử dụng thay vì 'username'
    public string id;  // Đảm bảo 'id' được sử dụng thay vì 'playerId'
    public string status;

    public DisplayResponse(string name, string id, string status)
    {
        this.name = name;
        this.id = id;
        this.status = status;
    }
}
