﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ExportManager : MonoBehaviour
{
    public void ExportToBackend()
    {
        string json = PlaceShip.ExportShipsAsGameJSON();
        Debug.Log("Ship JSON: " + json);

        StartCoroutine(SendShipData(json));
    }

    private IEnumerator SendShipData(string json)
    {
        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/gameplay/place-ship", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Gửi JWT token nếu có
        string token = PlayerPrefs.GetString("token", "");
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Gửi dữ liệu thành công! Phản hồi: " + request.downloadHandler.text);
        }
        else
        {
            {
                Debug.LogError($"❌ Lỗi gửi dữ liệu: {request.responseCode} - {request.downloadHandler.text}");

                if (request.responseCode == 400)
                {
                    Debug.LogError("⚠️ Dữ liệu không hợp lệ (gameId hoặc ships).");
                }
                else if (request.responseCode == 401)
                {
                    Debug.LogError("⚠️ Token không hợp lệ.");
                }
                else if (request.responseCode == 403)
                {
                    Debug.LogError("⚠️ Thiếu token xác thực.");
                }
                else if (request.responseCode == 500)
                {
                    Debug.LogError("💥 Lỗi phía server khi đặt tàu.");
                }
            }
        }
    }
}
    