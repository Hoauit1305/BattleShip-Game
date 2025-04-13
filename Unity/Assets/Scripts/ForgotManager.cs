using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class ForgotManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField gmailInput;

    public GameObject WrongText;
    public GameObject RemindText;

    public GameObject ForgotPwPanel;
    public GameObject LoginPanel;

    public void OnForgotPasswordButtonClicked()
    {
        WrongText.SetActive(false);
        RemindText.SetActive(false);

        string username = usernameInput.text.Trim();
        string email = gmailInput.text.Trim();
        if (username == "" || email == "")
        {
            RemindText.SetActive(true);
            return;
        }
        StartCoroutine(ForgotpwCoroutine(username, email));
    }

    IEnumerator ForgotpwCoroutine(string username, string email)
    {
        // Tạo JSON body
        string jsonBody = JsonUtility.ToJson(new ForgotpwRequestData(username, email));

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/forgot-password", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi request
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Gmail gửi thành công: " + request.downloadHandler.text);
            ForgotPwPanel.SetActive(false);
            LoginPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Gửi Gmail thất bại: " + request.error);
            WrongText.SetActive(true);
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
