using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    public TMP_Text NotifyText;

    public void OnLoginButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (username == "" || password == "")
        {
            NotifyText.text = "";
            NotifyText.text = "Vui lòng nhập đầy đủ thông tin !";
            return;
        }
        StartCoroutine(LoginCoroutine(username, password));
    }

    IEnumerator LoginCoroutine(string username, string password)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new LoginRequest(username, password));

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login thành công: " + request.downloadHandler.text);

            // Parse response JSON để lấy token
            string jsonResponse = request.downloadHandler.text;
            TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(jsonResponse);

            // Lưu token vào PlayerPrefs để dùng ở các nơi khác
            PlayerPrefs.SetString("token", tokenResponse.token);
            PlayerPrefs.Save();

            //Load sang Scene tiếp theo
            SceneManager.LoadScene("LobbyScene");
}
        else
        {
            Debug.LogError("Login thất bại: " + request.downloadHandler.text);
            NotifyText.text = "";
            NotifyText.text = "Sai tài khoản hoặc mật khẩu !";
        }
    }
}

[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;

    public LoginRequest(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}

[System.Serializable]
public class TokenResponse
{
    public string message;
    public string token;
}

