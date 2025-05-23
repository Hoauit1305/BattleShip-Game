﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Để gửi HTTP request
using UnityEngine.SceneManagement;
using System.Collections;

public class LogoutManager : MonoBehaviour
{
    public void OnLogoutButtonClicked()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        string token = PlayerPrefs.GetString("token", "");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("Không có token để đăng xuất.");
            yield break;
        }

        // Tạo request
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/auth/logout", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);

        // Gửi request
        yield return request.SendWebRequest();

        // Xử lý kết quả
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đăng xuất thành công!");

            // Xoá token lưu trong máy
            PlayerPrefs.DeleteKey("token");

            SceneManager.LoadScene("AuthScene");
        }
        else
        {
            Debug.LogError("Lỗi khi đăng xuất: " + request.error);
            Debug.LogError("Phản hồi từ server: " + request.downloadHandler.text);
        }
    }
}
