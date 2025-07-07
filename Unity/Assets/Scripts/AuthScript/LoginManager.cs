using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
//using ParrelSync;
public class LoginManager : MonoBehaviour
{
    public GameObject loadingPanel;
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
        
        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production.up.railway.app/api/auth/login", "POST");
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
            PrefsHelper.SetString("token", tokenResponse.token);
            PlayerPrefs.Save();

            // Kiểm tra có tên trong game chưa
            StartCoroutine(CheckNameCoroutine(username));
        }
        else
        {
            Debug.LogError("Login thất bại: " + request.downloadHandler.text);
            NotifyText.text = "";
            NotifyText.text = "Sai tài khoản hoặc mật khẩu !";
        }
    }
    IEnumerator CheckNameCoroutine(string username)
    {
        string jsonBody = JsonUtility.ToJson(new UsernameRequest(username));
        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production.up.railway.app/api/auth/check-name", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            NameResponse response = JsonUtility.FromJson<NameResponse>(request.downloadHandler.text);

            if (string.IsNullOrEmpty(response.name))
            {
                Debug.Log("Chưa có tên → chuyển sang SetNamePanel");
                PrefsHelper.SetInt("hasName", 0);  
            }
            else
            {
                Debug.Log("Đã có tên → vào LobbyScene");
                PrefsHelper.SetInt("hasName", 1);
                PrefsHelper.SetString("name", response.name);
            }
            PlayerPrefs.Save();
            // Hiện panel
            loadingPanel.SetActive(true);

            // Chờ 0.5 giây
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("LobbyScene");
        }
        else
        {
            Debug.LogError("Lỗi khi gọi /check-name: " + request.downloadHandler.text);
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

[System.Serializable]
public class UsernameRequest
{
    public string username;

    public UsernameRequest(string username)
    {
        this.username = username;
    }
}

[System.Serializable]
public class NameResponse
{
    public string name;
}