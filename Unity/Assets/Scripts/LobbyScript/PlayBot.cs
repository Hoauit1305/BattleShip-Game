using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System.Text;

public class PlayBot : MonoBehaviour
{
    public GameObject loadingPanel;

    public void OnClickFightBot()
    {
        StartCoroutine(SetIDAndStartMatch());
    }

    IEnumerator SetIDAndStartMatch()
    {
        string token = PrefsHelper.GetString("token");
        int playerId = PrefsHelper.GetInt("playerId");

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

        // ✅ Gửi yêu cầu tạo gameId (sửa URL này)
        SetIDRequest requestBody = new SetIDRequest(playerId);
        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production.up.railway.app/api/gameplay/create-gameid", "POST"); // ✅ ĐÃ SỬA
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đã setID trận đấu: " + request.downloadHandler.text);
            GameIDResponse response = JsonUtility.FromJson<GameIDResponse>(request.downloadHandler.text);

            int gameId = response.gameId;
            PrefsHelper.SetInt("gameId", gameId);
            Debug.Log("✅ Đã lưu gameId: " + gameId);

            // ✅ Gửi yêu cầu đặt tàu cho bot
            yield return StartCoroutine(PlaceShipsForBot(gameId, token));

            // Hiện panel & chuyển scene
            loadingPanel.SetActive(true);
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene("FindMatchesScene");
        }
        else
        {
            Debug.LogError("Lỗi khi setID: " + request.error + " | " + request.downloadHandler.text);
        }
    }

    IEnumerator PlaceShipsForBot(int gameId, string token)
    {
        PlaceBotShipsRequest shipRequest = new PlaceBotShipsRequest { gameId = gameId };
        string shipJson = JsonUtility.ToJson(shipRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(shipJson);

        UnityWebRequest request = new UnityWebRequest("https://battleship-game-production.up.railway.app/api/gameplay/place-ship/bot", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi khi đặt tàu cho bot: " + request.error + " | " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("✅ Đã đặt tàu cho bot: " + request.downloadHandler.text);
        }
    }
}

// --- Class hỗ trợ ---
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

[System.Serializable]
public class PlaceBotShipsRequest
{
    public int gameId;
}
