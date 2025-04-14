using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;


public class setname : MonoBehaviour
{
    public TMP_InputField nameInput;

    public GameObject RemindText;
    public GameObject LobbyPanel;

    public void OnFinalizeButtonClicked()
    {
        RemindText.SetActive(false);
        string name = nameInput.text;

        if (name == "")
        {
            RemindText.SetActive(true);
            return;
        }
        StartCoroutine(setNameCoroutine(name));
    }
    IEnumerator setNameCoroutine(string name)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new setNameRequest(name));
        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/choose-name", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đặt tên thành công: " + request.downloadHandler.text);

            LobbyPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Lỗi khi gọi API: " + request.error);
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