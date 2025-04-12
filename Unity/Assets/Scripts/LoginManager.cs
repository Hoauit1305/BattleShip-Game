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

    public GameObject WrongText;
    public GameObject RemindText;

    public void OnLoginButtonClicked()
    {
        WrongText.SetActive(false);
        RemindText.SetActive(false);
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (username == "" || password == "")
        {
            RemindText.SetActive(true);
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
         
            SceneManager.LoadScene("LobbyScene");
}
        else
        {
            Debug.LogError("Login thất bại: " + request.downloadHandler.text);
            WrongText.SetActive(true);
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
