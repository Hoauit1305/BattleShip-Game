    using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using ParrelSync;
using UnityEngine.InputSystem;

public class PlayBot : MonoBehaviour
{
    public void OnClickFightBot()
    {
        StartCoroutine(SetIDAndStartMatch());
    }

    IEnumerator SetIDAndStartMatch()
    {
        string token = PrefsHelper.GetString("token");
        int playerId = PrefsHelper.GetInt("playerId");  // lấy playerId từ PrefsHelper

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Thiếu token!");
            yield break;
        }
        if (playerId == 0)
        {
            Debug.LogError("Thiếu playerId!");
            yield break;
        }
        // Tạo JSON body chứa playerId
        SetIDRequest requestBody = new SetIDRequest(playerId);
        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest("http://localhost:3000/api/gameplay/set-id", "POST");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đã setID trận đấu: " + request.downloadHandler.text);
            // Parse JSON thủ công bằng JsonUtility (cần wrapper class)
            GameIDResponse response = JsonUtility.FromJson<GameIDResponse>(request.downloadHandler.text);
            // Lưu gameId vào PrefsHelper
            PrefsHelper.SetInt("gameId", response.gameId);
            Debug.Log("✅ Đã lưu gameId: " + response.gameId);

            SceneManager.LoadScene("FindMatchesScene");
        }
        else
        {
            Debug.LogError("Lỗi khi setID: " + request.error + " | " + request.downloadHandler.text);
        }
    }
}

// Class để serialize JSON body
[System.Serializable]
public class SetIDRequest
{
    public int playerId;

    public SetIDRequest(int playerId)
    {
        this.playerId = playerId;
    }
}
[System.Serializable]
public class GameIDResponse
{
    public string message;
    public int gameId;
}

