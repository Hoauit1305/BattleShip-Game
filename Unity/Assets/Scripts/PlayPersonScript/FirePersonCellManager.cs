using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

public class FirePersonCellManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject fireBotPanel; // Used as FirePersonPanel
    public GameObject botFirePanel; // Used as PersonFirePanel
    public GameObject winGamePanel;
    public GameObject loseGamePanel;
    public GameObject changeTurnPanel;

    // Ship prefabs
    public GameObject ship2Prefab;
    public GameObject ship31Prefab;
    public GameObject ship32Prefab;
    public GameObject ship4Prefab;
    public GameObject ship5Prefab;

    // Dictionary to track placed ships
    private Dictionary<string, GameObject> placedShips = new Dictionary<string, GameObject>();

    public static GameObject globalDiamond;
    public static bool isPlayerTurn = false;
    private List<GameObject> frameObjects = new List<GameObject>();
    public Color FrameColor = Color.red;

    // ✅ Thêm flag để kiểm tra xem đã qua giai đoạn đặt tàu chưa
    private bool isShipPlacementPhase = true;

    public static FirePersonCellManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

        DisableAllShipPrefabImages();
        if (winGamePanel) winGamePanel.SetActive(false);
        if (loseGamePanel) loseGamePanel.SetActive(false);

        // ✅ Ẩn cả hai panel khi bắt đầu (đang trong giai đoạn đặt tàu)
        if (fireBotPanel) WebSocketManager.Instance?.SetPanelVisible(fireBotPanel, false);
        if (botFirePanel) WebSocketManager.Instance?.SetPanelVisible(botFirePanel, false);

        // Hoãn phần grid sang coroutine:
        StartCoroutine(LateInit());

        // ✅ Bắt đầu coroutine kiểm tra PlaceShipPanel
        StartCoroutine(CheckPlaceShipPanelStatus());
    }

    // ✅ Coroutine kiểm tra trạng thái PlaceShipPanel
    private IEnumerator CheckPlaceShipPanelStatus()
    {
        GameObject placeShipPanel = null;

        // Đợi tìm PlaceShipPanel
        while (placeShipPanel == null)
        {
            placeShipPanel = GameObject.Find("PlaceShipPanel");
            yield return null;
        }

        Debug.Log("✅ Tìm thấy PlaceShipPanel, bắt đầu theo dõi...");

        // Theo dõi PlaceShipPanel cho đến khi nó bị tắt
        while (placeShipPanel != null && placeShipPanel.activeInHierarchy)
        {
            yield return null;
        }

        // PlaceShipPanel đã bị tắt -> kết thúc giai đoạn đặt tàu
        Debug.Log("✅ PlaceShipPanel đã tắt, kết thúc giai đoạn đặt tàu!");
        isShipPlacementPhase = false;

        // ➕ Gán lượt chơi đúng dựa vào role
        int myId = PrefsHelper.GetInt("playerId");
        int ownerId = PrefsHelper.GetInt("ownerId");
        int guestId = PrefsHelper.GetInt("guestId");

        FirePersonCellManager.isPlayerTurn = (myId == ownerId);
        Debug.Log($"👤 {(FirePersonCellManager.isPlayerTurn ? "Owner" : "Guest")} bắt đầu trước – isPlayerTurn = {FirePersonCellManager.isPlayerTurn}");

        // Bây giờ mới được hiển thị panel theo turn
        UpdatePanelVisibility();
    }

    private IEnumerator LateInit()
    {
        // Đợi cho tới khi ít nhất 1 frame trôi qua
        yield return null;

        // Hoặc đợi đến khi sinh đủ ô (ví dụ 100 ô cho bàn 10x10)
        while (GameObject.FindGameObjectsWithTag("GridCell").Length < 100)
            yield return null;   // chưa đủ, tiếp tục chờ

        // Giờ thì gắn trigger
        GameObject[] cells = GameObject.FindGameObjectsWithTag("GridCell_Fire");

        foreach (GameObject cell in cells)
        {
            if (!cell.TryGetComponent(out GridCellStatusPerson _))
                cell.AddComponent<GridCellStatusPerson>();

            if (!cell.TryGetComponent(out EventTrigger trigger))
                trigger = cell.AddComponent<EventTrigger>();

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ => OnCellPointerEnter(cell));
            AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => OnCellPointerExit());
            AddEventTrigger(trigger, EventTriggerType.PointerClick, _ => OnCellPointerClick(cell));
        }

        // ✅ KHÔNG gọi UpdatePanelVisibility() ở đây nữa
        Debug.Log("✅ FirePersonCellManager LateInit hoàn tất – đã gắn trigger cho " + cells.Length + " ô.");
    }

    void DisableAllShipPrefabImages()
    {
        if (ship2Prefab != null) ship2Prefab.GetComponent<Image>().enabled = false;
        if (ship31Prefab != null) ship31Prefab.GetComponent<Image>().enabled = false;
        if (ship32Prefab != null) ship32Prefab.GetComponent<Image>().enabled = false;
        if (ship4Prefab != null) ship4Prefab.GetComponent<Image>().enabled = false;
        if (ship5Prefab != null) ship5Prefab.GetComponent<Image>().enabled = false;
    }

    void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    void OnCellPointerEnter(GameObject cell)
    {
        if (!isPlayerTurn || isShipPlacementPhase) return; // ✅ Thêm check giai đoạn đặt tàu
        if (cell.GetComponent<GridCellStatusPerson>().isClicked)
            return;

        globalDiamond.transform.SetParent(cell.transform);
        globalDiamond.transform.localPosition = Vector3.zero;
        globalDiamond.GetComponent<Image>().enabled = true;
    }

    void OnCellPointerExit()
    {
        if (!isPlayerTurn || isShipPlacementPhase) return; // ✅ Thêm check giai đoạn đặt tàu
        globalDiamond.GetComponent<Image>().enabled = false;
    }

    void OnCellPointerClick(GameObject cell)
    {
        if (!isPlayerTurn || isShipPlacementPhase) return; // ✅ Thêm check giai đoạn đặt tàu

        GridCellStatusPerson status = cell.GetComponent<GridCellStatusPerson>();
        if (status != null && status.isClicked)
            return;

        isPlayerTurn = false;
        FireAudioManager.Instance?.PlayFireSound();
        if (status != null)
            status.isClicked = true;

        if (globalDiamond != null)
            globalDiamond.GetComponent<Image>().enabled = false;

        Transform existingRect = cell.transform.Find("Rectangle(Clone)");
        GameObject newRect;
        if (existingRect == null)
        {
            newRect = Instantiate(rectangleObject, cell.transform);
            newRect.name = "Rectangle";
            newRect.GetComponent<Image>().enabled = true;
            newRect.transform.localPosition = Vector3.zero;
        }
        else
        {
            newRect = existingRect.gameObject;
            newRect.GetComponent<Image>().enabled = true;
        }

        StartCoroutine(ChangeToCircleAndCallAPI(newRect, cell));
    }

    IEnumerator ChangeToCircleAndCallAPI(GameObject rectObj, GameObject cell)
    {
        yield return new WaitForSeconds(0.3f);

        if (rectObj != null)
        {
            Transform parent = rectObj.transform.parent;
            Vector3 pos = rectObj.transform.localPosition;
            Destroy(rectObj);

            string gameId = PrefsHelper.GetInt("gameId").ToString();
            string playerId = PrefsHelper.GetInt("playerId").ToString();
            string position = cell.name;
            string apiURL = "https://battleship-game-production-1176.up.railway.app/api/gameplay/fire-ship/person";

            ShotRequestPerson ShotRequestPerson = new ShotRequestPerson(gameId, playerId, position);
            UnityWebRequest request = CreatePostRequest(apiURL, ShotRequestPerson);

            Debug.Log($"Sending request to API with gameId: {gameId}, playerId: {playerId}, position: {position}");
            yield return request.SendWebRequest();

            string shotType = "miss";
            SunkShipPerson SunkShipPersonData = null;
            GameResultPerson GameResultPersonData = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"API response text: {responseText}");

                try
                {
                    var json = JSON.Parse(responseText);
                    var playerShot = json["playerShot"];
                    if (playerShot != null)
                    {
                        shotType = playerShot["result"];
                        string positionFromApi = playerShot["position"];
                        Debug.Log($"✅ Bắn vị trí: {positionFromApi}, result = {shotType}");

                        // Gán dữ liệu chìm tàu nếu có
                        if (playerShot["sunkShip"] != null)
                        {
                            JSONArray positionsJson = playerShot["sunkShip"]["positions"].AsArray;
                            List<string> positionsList = new List<string>();
                            foreach (JSONNode node in positionsJson)
                            {
                                positionsList.Add(node.Value);
                            }

                            SunkShipPersonData = new SunkShipPerson
                            {
                                shipId = playerShot["sunkShip"]["shipId"].AsInt,
                                shipType = playerShot["sunkShip"]["shipType"],
                                positions = positionsList.ToArray()
                            };
                        }

                        if (playerShot["gameResult"] != null)
                        {
                            GameResultPersonData = new GameResultPerson
                            {
                                status = playerShot["gameResult"]["status"],
                                winnerId = playerShot["gameResult"]["winnerId"].AsInt
                            };
                        }

                        WebSocketManager.Instance.SendFireResult(
                            int.Parse(cell.name.Substring(1)),       // cellX
                            cell.name[0] - 'A',                      // cellY
                            shotType,
                            SunkShipPersonData?.shipId ?? -1,
                            GameResultPersonData?.status == "completed" && GameResultPersonData.winnerId == PrefsHelper.GetInt("playerId")
                        );
                    }
                    else
                    {
                        Debug.LogError("❌ Không lấy được playerShot từ response JSON");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Lỗi parse JSON với SimpleJSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("API error: " + request.error);
            }

            // Handle sunk ship
            if (SunkShipPersonData != null && SunkShipPersonData.positions != null && SunkShipPersonData.positions.Length > 0)
            {
                ShowSunkShipPersonHighlights(SunkShipPersonData.positions);
                ShowSunkShipPerson(SunkShipPersonData);
            }
            else
            {
                GameObject circlePrefab = (shotType == "hit") ? circleRedObject : circleWhiteObject;
                if (circlePrefab != null)
                {
                    GameObject newCircle = Instantiate(circlePrefab, parent);
                    newCircle.name = "Circle";
                    newCircle.GetComponent<Image>().enabled = true;
                    newCircle.transform.localPosition = pos;
                }
            }

            // Handle game result
            if (GameResultPersonData != null && GameResultPersonData.status == "completed")
            {
                int currentPlayerId = PrefsHelper.GetInt("playerId");
                bool isWinner = GameResultPersonData.winnerId == currentPlayerId;
                Debug.Log($"Game result: {(isWinner ? "Win" : "Lose")}");
                yield return new WaitForSeconds(0.7f);
                ShowGameResultPersonPanel(isWinner);
                yield break;
            }

            if (shotType == "miss")
            {
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(FireShowChangeTurnPanel());
                // Hết lượt mình → ẩn FirePersonPanel, hiện PersonFirePanel
                WebSocketManager.Instance?.SetPanelVisible(fireBotPanel, false);
                WebSocketManager.Instance?.SetPanelVisible(botFirePanel, true);
                var showPlayerContainer = FindFirstObjectByType<ShowPlayerPersonContainer>();
                if (showPlayerContainer != null)
                    showPlayerContainer.RefreshPlayerShips();   
                UpdatePanelVisibility();

                
            }
            else
            {
                isPlayerTurn = true; // Vẫn tới lượt mình nếu trúng
                WebSocketManager.Instance?.SetPanelVisible(fireBotPanel, true);
                WebSocketManager.Instance?.SetPanelVisible(botFirePanel, false);
                UpdatePanelVisibility();
            }
        }
    }

    public void UpdatePanelVisibility()
    {
        // ✅ Chỉ hiển thị panel khi đã qua giai đoạn đặt tàu
        if (isShipPlacementPhase)
        {
            WebSocketManager.Instance?.SetPanelVisible(fireBotPanel, false);
            WebSocketManager.Instance?.SetPanelVisible(botFirePanel, false);
            Debug.Log("[UpdatePanelVisibility] Đang trong giai đoạn đặt tàu - ẩn tất cả panel");
            return;
        }

        WebSocketManager.Instance?.SetPanelVisible(fireBotPanel, isPlayerTurn);
        WebSocketManager.Instance?.SetPanelVisible(botFirePanel, !isPlayerTurn);
        changeTurnPanel.SetActive(false);

        Debug.Log($"[CanvasGroup] FirePersonPanel={(isPlayerTurn ? "Visible" : "Hidden")}, PersonFirePanel={(!isPlayerTurn ? "Visible" : "Hidden")}");
    }

    public IEnumerator HandleOpponentFire(FireResultPerson shot)
    {
        if (string.IsNullOrEmpty(shot.position))
        {
            // Tự tính lại nếu thiếu
            char rowChar = (char)('A' + shot.cellY);   // cellY là hàng
            shot.position = $"{rowChar}{shot.cellX}";
            Debug.Log($"🛠 Đã tự tính lại position: {shot.position}");
        }

        Debug.Log($"Opponent fired at: {shot.position}, result: {shot.result}");

        // Đợi GridCell sinh ra
        float waitTime = 0f;
        GameObject[] allCells = null;
        while ((allCells = GameObject.FindGameObjectsWithTag("GridCell_Person")).Length == 0 && waitTime < 3f)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        GameObject cell = allCells.FirstOrDefault(c => c.name == shot.position);
        if (cell == null)
        {
            Debug.LogError($"❌ Cell not found: {shot.position}");
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

        if (shot.SunkShipPerson?.positions != null && shot.SunkShipPerson.positions.Length > 0)
        {
            ShowSunkShipPersonHighlights(shot.SunkShipPerson.positions);
            ShowSunkShipPerson(shot.SunkShipPerson);
        }
        else
        {
            GameObject circlePrefab = (shot.result == "hit") ? circleRedObject : circleWhiteObject;
            GameObject circle = Instantiate(circlePrefab, cell.transform);
            circle.GetComponent<Image>().enabled = true;
            circle.transform.localPosition = Vector3.zero;
        }

        if (shot.GameResultPerson != null && shot.GameResultPerson.status == "completed")
        {
            int currentPlayerId = PrefsHelper.GetInt("playerId");
            bool isWin = shot.GameResultPerson.winnerId == currentPlayerId;
            ShowGameResultPersonPanel(isWin);
            yield break;
        }
    }

    void ShowGameResultPersonPanel(bool isWin)
    {
        if (winGamePanel == null || loseGamePanel == null)
        {
            Debug.LogError("Game result panels have not been assigned in the inspector!");
            return;
        }

        fireBotPanel.SetActive(false);
        botFirePanel.SetActive(false);
        changeTurnPanel.SetActive(false);

        if (isWin)
        {
            winGamePanel.SetActive(true);
            loseGamePanel.SetActive(false);
            winGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(winGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }
        else
        {
            loseGamePanel.SetActive(true);
            winGamePanel.SetActive(false);
            loseGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(loseGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }

        Debug.Log($"Showing game result panel: {(isWin ? "Win" : "Lose")}");
    }

    void ShowSunkShipPersonHighlights(string[] positions)
    {
        Transform parentTransform = fireBotPanel.transform;
        foreach (string positionName in positions)
        {
            GameObject cell = GameObject.Find(positionName);
            if (cell != null)
            {
                GameObject frame = new GameObject("FireBotHighlight");
                RectTransform frameTransform = frame.AddComponent<RectTransform>();
                frame.transform.SetParent(parentTransform);

                RectTransform cellRect = cell.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    frameTransform.position = cell.transform.position;
                    frameTransform.sizeDelta = cellRect.sizeDelta;
                    frameTransform.localScale = Vector3.one;

                    Image frameImage = frame.AddComponent<Image>();
                    frameImage.color = new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.4f);
                    frameObjects.Add(frame);
                    Debug.Log($"Created highlight for cell {positionName}");
                }
            }
        }
    }

    void ShowSunkShipPerson(SunkShipPerson SunkShipPerson)
    {
        Debug.Log($"Showing sunk ship: {SunkShipPerson.shipType} at {string.Join(", ", SunkShipPerson.positions)}");

        GameObject shipPrefab = GetShipPrefabByType(SunkShipPerson.shipType);
        if (shipPrefab == null)
        {
            Debug.LogError($"No prefab found for ship: {SunkShipPerson.shipType}");
            return;
        }

        string shipKey = SunkShipPerson.shipId.ToString();
        GameObject shipInstance;

        if (placedShips.ContainsKey(shipKey))
        {
            shipInstance = placedShips[shipKey];
            Debug.Log($"Ship {shipKey} already exists, moving to new position");
        }
        else
        {
            GameObject firstCell = GameObject.Find(SunkShipPerson.positions[0]);
            if (firstCell == null)
            {
                Debug.LogError($"Cell not found: {SunkShipPerson.positions[0]}");
                return;
            }

            shipInstance = Instantiate(shipPrefab, firstCell.transform);
            shipInstance.name = SunkShipPerson.shipType;
            placedShips.Add(shipKey, shipInstance);
            Debug.Log($"Created new ship {shipKey}");
        }

        foreach (string positionName in SunkShipPerson.positions)
        {
            GameObject cell = GameObject.Find(positionName);
            if (cell != null)
            {
                Transform circleTransform = cell.transform.Find("Circle");
                if (circleTransform != null)
                {
                    Destroy(circleTransform.gameObject);
                }
            }
        }

        GameObject startCell = GameObject.Find(SunkShipPerson.positions[0]);
        if (startCell != null)
        {
            shipInstance.transform.SetParent(startCell.transform);
            shipInstance.transform.localPosition = Vector3.zero;

            bool isVertical = IsShipVertical(SunkShipPerson.positions);
            ConfigureShipVisual(shipInstance, SunkShipPerson, isVertical);

            Image shipImage = shipInstance.GetComponent<Image>();
            if (shipImage != null)
            {
                shipImage.enabled = true;
                Debug.Log($"Enabled image for ship {SunkShipPerson.shipType}");
            }
            else
            {
                Debug.LogError($"No Image component on ship {SunkShipPerson.shipType}");
            }
        }
        else
        {
            Debug.LogError($"Start cell not found: {SunkShipPerson.positions[0]}");
        }
    }

    bool IsShipVertical(string[] positions)
    {
        if (positions.Length <= 1) return false;
        return positions[0][0] != positions[1][0];
    }

    GameObject GetShipPrefabByType(string shipType)
    {
        switch (shipType)
        {
            case "Ship2": return ship2Prefab;
            case "Ship3.1": return ship31Prefab;
            case "Ship3.2": return ship32Prefab;
            case "Ship4": return ship4Prefab;
            case "Ship5": return ship5Prefab;
            default: return null;
        }
    }

    void ConfigureShipVisual(GameObject shipObject, SunkShipPerson SunkShipPerson, bool isVertical)
    {
        RectTransform rectTransform = shipObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        GameObject firstCell = GameObject.Find(SunkShipPerson.positions[0]);
        RectTransform cellRect = firstCell?.GetComponent<RectTransform>();
        if (cellRect == null) return;
        float cellSize = cellRect.rect.width;

        int shipSize = GetShipSizeFromType(SunkShipPerson.shipType);

        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        float scaleFactor = 0.9f;
        rectTransform.sizeDelta = new Vector2(cellSize * shipSize * scaleFactor, cellSize * scaleFactor);

        if (isVertical)
        {
            rectTransform.Rotate(0, 0, 90);
        }

        string firstCellName = SunkShipPerson.positions[0];
        string lastCellName = SunkShipPerson.positions[SunkShipPerson.positions.Length - 1];

        GameObject firstCellObj = GameObject.Find(firstCellName);
        GameObject lastCellObj = GameObject.Find(lastCellName);

        if (firstCellObj != null && lastCellObj != null)
        {
            Vector3 firstPos = firstCellObj.transform.position;
            Vector3 lastPos = lastCellObj.transform.position;
            Vector3 centerPos = (firstPos + lastPos) / 2f;
            rectTransform.position = centerPos;
        }

        shipObject.transform.SetAsLastSibling();

        Image shipImage = shipObject.GetComponent<Image>();
        if (shipImage != null)
        {
            shipImage.raycastTarget = false;
            if (shipImage is UnityEngine.UI.Image uiImage && uiImage.type == UnityEngine.UI.Image.Type.Sliced)
            {
                uiImage.pixelsPerUnitMultiplier = 1;
            }
        }
    }

    int GetShipSizeFromType(string shipType)
    {
        switch (shipType)
        {
            case "Ship2": return 2;
            case "Ship3.1":
            case "Ship3.2": return 3;
            case "Ship4": return 4;
            case "Ship5": return 5;
            default: return 1;
        }
    }

    public IEnumerator FireShowChangeTurnPanel()
    {
        if (changeTurnPanel != null)
        {
            Debug.Log("Fire ChangeTurnPanel đã hiển thị");
            changeTurnPanel.SetActive(true);
            changeTurnPanel.transform.SetAsLastSibling();
            changeTurnPanel.transform.localScale = Vector3.zero;
            LeanTween.scale(changeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();
            yield return new WaitForSeconds(1.2f);
            changeTurnPanel.SetActive(false);
        }
        else
        {
            yield return null;
            Debug.Log("ChangeTurnPanel is null");
        }
    }

    UnityWebRequest CreatePostRequest(string url, ShotRequestPerson ShotRequestPerson)
    {
        string jsonBody = JsonUtility.ToJson(ShotRequestPerson);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        return request;
    }

    int GetOpponentId(string gameId, int currentPlayerId)
    {
        // This assumes you have a way to retrieve game data, e.g., from PrefsHelper or API
        // For simplicity, assuming game data is stored in PrefsHelper
        string gameData = PrefsHelper.GetString("gameData");
        if (!string.IsNullOrEmpty(gameData))
        {
            JSONNode gameJson = JSON.Parse(gameData);
            int playerId1 = gameJson["Player_Id_1"].AsInt;
            int playerId2 = gameJson["Player_Id_2"].AsInt;
            return currentPlayerId == playerId1 ? playerId2 : playerId1;
        }
        return -1; // Fallback, should be handled with proper error
    }
}

[System.Serializable]
public class ShotRequestPerson
{
    public string gameId;
    public string playerId;
    public string position;

    public ShotRequestPerson(string gameId, string playerId, string position)
    {
        this.gameId = gameId;
        this.playerId = playerId;
        this.position = position;
    }
}

[System.Serializable]
public class GameResultPerson
{
    public string status;
    public int winnerId;
}

[System.Serializable]
public class PlayerShotPerson
{
    public string position;
    public string result;
    public SunkShipPerson SunkShipPerson;
    public GameResultPerson GameResultPerson;
}

[System.Serializable]
public class ShotResponsePerson
{
    public string message;
    public PlayerShotPerson PlayerShotPerson;
}

[System.Serializable]
public class SunkShipPerson
{
    public int shipId;
    public string shipType;
    public string[] positions;
}

[System.Serializable]
public class FireResultPerson
{
    public string type;
    public string position;
    public string result;
    public SunkShipPerson SunkShipPerson;
    public GameResultPerson gameResult;
    public int cellX;
    public int cellY;

    public GameResultPerson GameResultPerson => gameResult;
}

public class GridCellStatusPerson : MonoBehaviour
{
    public bool isClicked = false;
}