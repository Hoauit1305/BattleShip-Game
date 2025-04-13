using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class ChangePwManager : MonoBehaviour
{
    public GameObject ChangePwPanel;
    public GameObject LoginPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField oldPasswordInput;
    public TMP_InputField newPasswordInput;

    public GameObject WrongText;
    public GameObject RemindText;

    public void OnChangePwButtonClicked()
    {
        WrongText.SetActive(false);
        RemindText.SetActive(false);
        string username = usernameInput.text;
        string oldPassword = oldPasswordInput.text;
        string newPassword = newPasswordInput.text;

        if (username == "" || oldPassword == "" || newPassword == "")
        {
            RemindText.SetActive(true);
            return;
        }
        StartCoroutine(ChangePwCoroutine(username, oldPassword, newPassword));
    }

    IEnumerator ChangePwCoroutine(string username, string oldPassword, string newPassword)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new ChangePwRequest(username, oldPassword, newPassword));

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/change-password", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đổi mật khẩu thành công" + request.downloadHandler.text);
            ChangePwPanel.SetActive(false);
            LoginPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Đổi mật khẩu thất bại" + request.downloadHandler.text);
            WrongText.SetActive(true);
        }
    }
}
 
[System.Serializable]
public class ChangePwRequest
{
    public string username;
    public string oldPassword;
    public string newPassword;


    public ChangePwRequest(string username, string oldPassword, string newPassword)
    {
        this.username = username;
        this.oldPassword = oldPassword;
        this.newPassword = newPassword;
    }
}
