// ✅ ĐÃ CHỈNH HOÀN TOÀN ĐỂ LUÂN PHIÊN BẮN GIỮA 2 NGƯỜI
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WebSocketSharp;
using SimpleJSON;

public class PersonFireCellManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject firePanel;
    public GameObject PersonChangeTurnPanel;
    public GameObject winGamePanel;
    public GameObject loseGamePanel;

    public GameObject ship2Prefab;
    public GameObject ship31Prefab;
    public GameObject ship32Prefab;
    public GameObject ship4Prefab;
    public GameObject ship5Prefab;

    public WebSocket socket;
    public static PersonFireCellManager Instance;
    private void Awake()
    {
        Instance = this; // ✅ Gán thể hiện singleton
    }
    private void Start()
    {
        if (socket != null && socket.IsAlive)
        {
            socket.OnMessage += (sender, e) =>
            {
                Debug.Log("[SOCKET] Nhận message: " + e.Data);
                JSONNode data = JSON.Parse(e.Data);

                if (data["type"] == "fire_result")
                {
                    FireResult result = JsonUtility.FromJson<FireResult>(e.Data);
                    StartCoroutine(HandleOpponentFire(result));
                }
                else if (data["type"] == "switch_turn")
                {
                    int myId = PrefsHelper.GetInt("playerId");
                    int toPlayer = data["toPlayerId"].AsInt;
                    if (toPlayer == myId)
                    {
                        Debug.Log("[SOCKET] Nhận switch_turn, đến lượt mình!");

                        firePanel.SetActive(false); // Ẩn panel cũ trước
                        StartCoroutine(ShowChangeTurnPanelThenNotifyManager());
                    }

                }
            };
        }

        if (winGamePanel != null) winGamePanel.SetActive(false);
        if (loseGamePanel != null) loseGamePanel.SetActive(false);
    }
    private IEnumerator ShowChangeTurnPanelThenNotifyManager()
    {
        yield return StartCoroutine(PersonShowChangeTurnPanel());

        // ✅ Gọi lại SwitchToPlayerTurn ở WebSocketManager (nếu cần)
        // Hoặc bật FirePersonPanel ở đây nếu bạn không dùng WebSocketManager nữa
        FirePersonCellManager.isPlayerTurn = true;
        FirePersonCellManager.Instance?.UpdatePanelVisibility();
    }

    private IEnumerator HandleOpponentFire(FireResult shot)
    {
        Debug.Log("Opponent bắn tại: " + shot.position + " | Kết quả: " + shot.result);

        GameObject cell = GameObject.Find(shot.position);
        if (cell == null)
        {
            Debug.LogError("Không tìm thấy ô: " + shot.position);
            yield break;
        }

        FireAudioManager.Instance?.PlayFireSound();

        GameObject diamond = Instantiate(diamondObject, cell.transform);
        diamond.GetComponent<Image>().enabled = true;
        diamond.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(0.5f);
        Destroy(diamond);

        GameObject rectangle = Instantiate(rectangleObject, cell.transform);
        rectangle.GetComponent<Image>().enabled = true;
        rectangle.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(0.5f);
        Destroy(rectangle);

        if (shot.SunkShip1 != null && shot.SunkShip1.positions != null && shot.SunkShip1.positions.Length > 0)
        {
            ShowSunkShip1Highlights(shot.SunkShip1.positions);
            ShowSunkShip1(shot.SunkShip1);
        }
        else
        {
            GameObject circlePrefab = (shot.result == "hit") ? circleRedObject : circleWhiteObject;
            GameObject circle = Instantiate(circlePrefab, cell.transform);
            circle.GetComponent<Image>().enabled = true;
            circle.transform.localPosition = Vector3.zero;
        }

        if (shot.GameResult1 != null && shot.GameResult1.status == "completed")
        {
            int currentPlayerId = PrefsHelper.GetInt("playerId");
            bool isWin = shot.GameResult1.winnerId == currentPlayerId;
            ShowGameResult1Panel(isWin);
            yield break;
        }

        yield return StartCoroutine(PersonShowChangeTurnPanel());
        firePanel.SetActive(true);
    }

    private void ShowGameResult1Panel(bool isWin)
    {
        firePanel.SetActive(false);
        PersonChangeTurnPanel.SetActive(false);

        if (isWin)
        {
            winGamePanel.SetActive(true);
            winGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(winGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }
        else
        {
            loseGamePanel.SetActive(true);
            loseGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(loseGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }
    }

    public IEnumerator PersonShowChangeTurnPanel()
    {
        if (PersonChangeTurnPanel != null)
        {
            Debug.Log("Person ChangeTurnPanel đã hiển thị");

            PersonChangeTurnPanel.SetActive(true);
            PersonChangeTurnPanel.transform.localScale = Vector3.zero;
            LeanTween.scale(PersonChangeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();

            yield return new WaitForSeconds(1.2f);
            PersonChangeTurnPanel.SetActive(false);
        }
        else
        {
            yield return null;
        }
    }

    private void ShowSunkShip1Highlights(string[] positions)
    {
        Transform parentTransform = firePanel.transform;
        foreach (string pos in positions)
        {
            GameObject cell = GameObject.Find(pos);
            if (cell != null)
            {
                GameObject frame = new GameObject("Highlight");
                RectTransform frameTransform = frame.AddComponent<RectTransform>();
                frame.transform.SetParent(parentTransform);

                RectTransform cellRect = cell.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    frameTransform.position = cell.transform.position;
                    frameTransform.sizeDelta = cellRect.sizeDelta;
                    frameTransform.localScale = Vector3.one;

                    Image frameImage = frame.AddComponent<Image>();
                    frameImage.color = new Color(1f, 0f, 0f, 0.4f);
                }
            }
        }
    }

    private void ShowSunkShip1(SunkShip1 SunkShip1)
    {
        GameObject shipPrefab = GetShipPrefabByType(SunkShip1.shipType);
        if (shipPrefab == null) return;

        GameObject shipInstance = Instantiate(shipPrefab);
        shipInstance.name = SunkShip1.shipType;

        GameObject startCell = GameObject.Find(SunkShip1.positions[0]);
        if (startCell != null)
        {
            shipInstance.transform.SetParent(startCell.transform);
            shipInstance.transform.localPosition = Vector3.zero;
            bool isVertical = IsShipVertical(SunkShip1.positions);
            ConfigureShipVisual(shipInstance, SunkShip1, isVertical);
            shipInstance.GetComponent<Image>().enabled = true;
        }
    }

    private bool IsShipVertical(string[] positions)
    {
        return positions.Length > 1 && positions[0][0] != positions[1][0];
    }

    private GameObject GetShipPrefabByType(string shipType)
    {
        return shipType switch
        {
            "Ship2" => ship2Prefab,
            "Ship3.1" => ship31Prefab,
            "Ship3.2" => ship32Prefab,
            "Ship4" => ship4Prefab,
            "Ship5" => ship5Prefab,
            _ => null,
        };
    }

    private void ConfigureShipVisual(GameObject shipObject, SunkShip1 SunkShip1, bool isVertical)
    {
        RectTransform rectTransform = shipObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        GameObject firstCell = GameObject.Find(SunkShip1.positions[0]);
        RectTransform cellRect = firstCell?.GetComponent<RectTransform>();
        if (cellRect == null) return;

        float cellSize = cellRect.rect.width;
        int shipSize = SunkShip1.positions.Length;

        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        float scaleFactor = 0.9f;
        rectTransform.sizeDelta = new Vector2(cellSize * shipSize * scaleFactor, cellSize * scaleFactor);

        if (isVertical)
        {
            rectTransform.Rotate(0, 0, 90);
        }
    }
}

[System.Serializable]
public class FireResult
{
    public string position;
    public string result;
    public SunkShip1 SunkShip1;
    public GameResult1 GameResult1;
}

[System.Serializable]
public class SunkShip1
{
    public int shipId;
    public string shipType;
    public string[] positions;
}

[System.Serializable]
public class GameResult1
{
    public string status;
    public int winnerId;
}
