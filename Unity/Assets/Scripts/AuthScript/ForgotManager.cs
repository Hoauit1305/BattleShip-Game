using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class ForgotManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField gmailInput;

    public TMP_Text NotifyText;

    public void OnForgotPasswordButtonClicked()
    {
        string username = usernameInput.text.Trim();
        string email = gmailInput.text.Trim();
        if (username == "" || email == "")
        {
            NotifyText.text = "";
            NotifyText.text = "Vui lòng nhập đầy đủ thông tin !";
            return;
        }
        StartCoroutine(ForgotpwCoroutine(username, email));
    }

    IEnumerator ForgotpwCoroutine(string username, string email)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new ForgotpwRequestData(username, email));

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production-1176.up.railway.app/api/auth/forgot-password", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi request
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Gmail gửi thành công: " + request.downloadHandler.text);
            NotifyText.text = "";
            NotifyText.text = "Mật khẩu đã được gửi về gmail !";
        }
        else
        {
            Debug.LogError("Gửi Gmail thất bại: " + request.error);
            NotifyText.text = "";
            NotifyText.text = "Sai tài khoản hoặc gmail !";
        }
    }
}

[System.Serializable]
public class ForgotpwRequestData
{
    public string username;
    public string email;

    public ForgotpwRequestData(string username, string email)
    {
        this.username = username;
        this.email = email;
    }
}
