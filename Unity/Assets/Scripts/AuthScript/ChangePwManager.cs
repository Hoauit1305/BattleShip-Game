using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class ChangePwManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField oldPasswordInput;
    public TMP_InputField newPasswordInput;

    public TMP_Text NotifyText;

    public void OnChangePwButtonClicked()
    {
        string username = usernameInput.text;
        string oldPassword = oldPasswordInput.text;
        string newPassword = newPasswordInput.text;

        if (username == "" || oldPassword == "" || newPassword == "")
        {
            NotifyText.text = "";
            NotifyText.text = "Vui lòng nhập đầy đủ thông tin !";
            return;
        }
        StartCoroutine(ChangePwCoroutine(username, oldPassword, newPassword));
    }

    IEnumerator ChangePwCoroutine(string username, string oldPassword, string newPassword)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new ChangePwRequest(username, oldPassword, newPassword));

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production-1176.up.railway.app/api/auth/change-password", "POST");
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

            NotifyText.text = "";
            NotifyText.text = "Đổi mật khẩu thành công !";
        }
        else
        {
            Debug.LogError("Đổi mật khẩu thất bại" + request.downloadHandler.text);
            
            NotifyText.text = "";
            NotifyText.text = "Sai tài khoản hoặc mật khẩu cũ !";
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
