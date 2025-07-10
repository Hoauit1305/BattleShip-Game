using System.Collections;
using UnityEngine;
using UnityEngine.UI; // dùng Text thường
using TMPro;

public class CountdownPersonManager : MonoBehaviour
{
    public TMP_Text countdownText; // hoặc TMP_Text nếu dùng TMP
    public GameObject CountdownPanel;

    public void OnFinishPlacingShips()
    {
        // Gửi socket báo đã sẵn sàng
        int gameId = PrefsHelper.GetInt("gameId");
        int playerId = PrefsHelper.GetInt("playerId");
        int opponentId = PrefsHelper.GetInt("opponentId");

        string json = $"{{" +
            $"\"action\":\"ready_place_ship\"," +
            $"\"gameId\":{gameId}," +
            $"\"playerId\":{playerId}," +
            $"\"opponentId\":{opponentId}" +
        $"}}";

        WebSocketManager.Instance.SendRawJson(json);
        Debug.Log("📤 Đã gửi ready_place_ship");
    }

    public IEnumerator StartCountdown(System.Action onComplete)
    {
        CountdownPanel.SetActive(true);
        string[] steps = { "3", "2", "1", "READY", "GO!" };

        foreach (string step in steps)
        {
            countdownText.text = step;
            countdownText.transform.localScale = Vector3.zero;
            LeanTween.scale(countdownText.gameObject, Vector3.one, 0.3f).setEaseOutBack();
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
        CountdownPanel.SetActive(false);
        onComplete?.Invoke(); // gọi hành động tiếp theo (bắt đầu game)

        int playerId = PrefsHelper.GetInt("playerId");
        int ownerId = PrefsHelper.GetInt("ownerId");

        if (playerId == ownerId)
        {
            Debug.Log("🚀 Player này là owner — chuyển sang panel bắn tàu.");
            // Enable FirePersonPanel
            GameObject.Find("FirePersonPanel").SetActive(true);
            GameObject.Find("PersonFirePanel").SetActive(false);
        }
        else
        {
            Debug.Log("👀 Player này là guest — chuyển sang panel xem owner bắn.");
            // Enable PersonFirePanel
            GameObject.Find("FirePersonPanel").SetActive(false);
            GameObject.Find("PersonFirePanel").SetActive(true);
        }
    }
}