using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;


public class setname : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_Text NotifyText;
    public GameObject LobbyPanel;
    public GameObject ChooseNamePanel;

    public void OnFinalizeButtonClicked()
    {
        string name = nameInput.text;

        if (name == "")
        {
            NotifyText.text = "";
            NotifyText.text = "Vui lòng nhập đầy đủ thông tin !";
            return;
        }
        StartCoroutine(setNameCoroutine(name));
    }
    IEnumerator setNameCoroutine(string name)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new setNameRequest(name));
        // Tạo request
        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production.up.railway.app/api/auth/choose-name", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Thêm Authorization token
        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả    
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đặt tên thành công: " + request.downloadHandler.text);
            ChooseNamePanel.SetActive(false);
            LobbyPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Lỗi khi gọi API: " + request.error);
            Debug.Log("Phản hồi từ server: " + request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class setNameRequest
{
    public string name;

    public setNameRequest(string name)
    {
        this.name = name;
    }
}