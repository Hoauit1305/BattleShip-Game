using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class RegisterManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField rePasswordInput;

    public TMP_Text NotifyText;

    public void OnRegisterButtonClicked()
    {
        string username = usernameInput.text;
        string email = emailInput.text;
        string password = passwordInput.text;
        string repassword = rePasswordInput.text;

        if(username == "" || email == "" || password == "" || repassword == "")
        {
            NotifyText.text = "";
            NotifyText.text = "Vui lòng nhập đầy đủ thông tin !";
            return;
        }

        if(password != repassword)
        {
            NotifyText.text = "";
            NotifyText.text = "Mật khẩu nhập lại không đúng !";
            return;
        }

        StartCoroutine(RegisterCoroutine(username, email, password));
    }

    IEnumerator RegisterCoroutine(string username, string email, string password)
    {
        string jsonBody = JsonUtility.ToJson(new RegisterRequest(username, email, password));

        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/register", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Register thành công: " + request.downloadHandler.text);

                NotifyText.text = "";
                NotifyText.text = "Đăng ký thành công !";
            }
            else
            {
                int statusCode = (int)request.responseCode;
                string serverMessage = request.downloadHandler.text;

                Debug.LogError("Register thất bại: " + serverMessage);

                if (statusCode == 500)
                {
                    NotifyText.text = "";
                    NotifyText.text = "Lỗi server! Vui lòng thử lại sau.";
                }
                else if (statusCode == 400)
                {
                    NotifyText.text = "";
                    NotifyText.text = "Tên tài khoản hoặc email đã tồn tại !";
                }
        }
    }
}

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;

    public RegisterRequest(string username, string email, string password)
    {
        this.username = username;
        this.email = email;
        this.password = password;
    }
}
