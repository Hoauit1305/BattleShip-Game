using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

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
    public static bool isPlayerTurn = true;
    private List<GameObject> frameObjects = new List<GameObject>();
    public Color FrameColor = Color.red;

    public static FirePersonCellManager Instance;

    void Start()
    {
        Instance = this;

        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

        // Disable ship prefab images
        DisableAllShipPrefabImages();

        // Hide game result panels
        if (winGamePanel != null) winGamePanel.SetActive(false);
        if (loseGamePanel != null) loseGamePanel.SetActive(false);

        // Initialize grid cells with event triggers
        GameObject[] cells = GameObject.FindGameObjectsWithTag("GridCell");
        foreach (GameObject cell in cells)
        {
            if (cell.GetComponent<GridCellStatusPerson>() == null)
                cell.AddComponent<GridCellStatusPerson>();

            EventTrigger trigger = cell.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cell.AddComponent<EventTrigger>();

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, (eventData) => { OnCellPointerEnter(cell); });
            AddEventTrigger(trigger, EventTriggerType.PointerExit, (eventData) => { OnCellPointerExit(); });
            AddEventTrigger(trigger, EventTriggerType.PointerClick, (eventData) => { OnCellPointerClick(cell); });
        }

        // Set initial panel visibility based on turn
        UpdatePanelVisibility();

        // Set up WebSocket message handling
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
        if (!isPlayerTurn) return;
        if (cell.GetComponent<GridCellStatusPerson>().isClicked)
            return;

        globalDiamond.transform.SetParent(cell.transform);
        globalDiamond.transform.localPosition = Vector3.zero;
        globalDiamond.GetComponent<Image>().enabled = true;
    }

    void OnCellPointerExit()
    {
        if (!isPlayerTurn) return;
        globalDiamond.GetComponent<Image>().enabled = false;
    }

    void OnCellPointerClick(GameObject cell)
    {
        if (!isPlayerTurn) return;

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
                    ShotResponsePerson response = JsonUtility.FromJson<ShotResponsePerson>(responseText);

                    if (response != null && response.PlayerShotPerson != null)
                    {
                        shotType = response.PlayerShotPerson.result;
                        SunkShipPersonData = response.PlayerShotPerson.SunkShipPerson;
                        GameResultPersonData = response.PlayerShotPerson.GameResultPerson;

                        Debug.Log($"Parsed response - message: {response.message}, position: {response.PlayerShotPerson.position}, result: {shotType}");

                        if (SunkShipPersonData != null)
                        {
                            Debug.Log($"SunkShipPerson: id={SunkShipPersonData.shipId}, type={SunkShipPersonData.shipType}, positions: {string.Join(", ", SunkShipPersonData.positions)}");
                        }
                        if (GameResultPersonData != null)
                        {
                            Debug.Log($"Game result: status={GameResultPersonData.status}, winnerId={GameResultPersonData.winnerId}");
                        }

                        var FireResultPerson = new FireResultPerson
                        {
                            type = "fire_result",
                            position = response.PlayerShotPerson.position,
                            result = response.PlayerShotPerson.result,
                            SunkShipPerson = response.PlayerShotPerson.SunkShipPerson,
                            GameResultPerson = response.PlayerShotPerson.GameResultPerson
                        };

                        string jsonFireResultPerson = JsonUtility.ToJson(FireResultPerson);
                        Debug.Log($"[WS] Gửi fire_result: {jsonFireResultPerson}");
                        WebSocketManager.Instance?.SendRawJson(jsonFireResultPerson);

                        // Nếu là "miss" thì chuyển lượt
                        if (shotType == "miss")
                        {
                            int opponentId = PrefsHelper.GetInt("opponentId");

                            var switchTurn = new
                            {
                                type = "switch_turn",
                                fromPlayerId = int.Parse(playerId),
                                toPlayerId = opponentId
                            };

                            string jsonSwitchTurn = JsonUtility.ToJson(switchTurn);
                            Debug.Log($"[WS] Gửi switch_turn: {jsonSwitchTurn}");
                            WebSocketManager.Instance?.SendRawJson(jsonSwitchTurn);
                        }

                    }
                    else
                    {
                        Debug.LogError("Error: response or PlayerShotPerson is null after parsing");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing JSON: {e.Message}");
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

            // Update panel visibility if shot is a miss
            if (shotType == "miss")
            {
                UpdatePanelVisibility();
            }
            else
            {
                isPlayerTurn = true; // Continue shooting if hit
                UpdatePanelVisibility();
            }
        }
    }

    public void UpdatePanelVisibility()
    {
        fireBotPanel.SetActive(isPlayerTurn);
        botFirePanel.SetActive(!isPlayerTurn);
        changeTurnPanel.SetActive(false);
        Debug.Log($"Panel visibility updated: FirePersonPanel={(isPlayerTurn ? "Active" : "Inactive")}, PersonFirePanel={(!isPlayerTurn ? "Active" : "Inactive")}");
    }

    public IEnumerator HandleOpponentFire(FireResultPerson shot)
    {
        Debug.Log($"Opponent fired at: {shot.position}, result: {shot.result}");

        GameObject cell = GameObject.Find(shot.position);
        if (cell == null)
        {
            Debug.LogError($"Cell not found: {shot.position}");
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

        if (shot.SunkShipPerson != null && shot.SunkShipPerson.positions != null && shot.SunkShipPerson.positions.Length > 0)
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

    public IEnumerator ShowChangeTurnPanel()
    {
        if (changeTurnPanel != null)
        {
            changeTurnPanel.SetActive(true);
            changeTurnPanel.transform.localScale = Vector3.zero;
            LeanTween.scale(changeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();
            yield return new WaitForSeconds(1.2f);
            changeTurnPanel.SetActive(false);
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
    public GameResultPerson GameResultPerson;
}

public class GridCellStatusPerson : MonoBehaviour
{
    public bool isClicked = false;
}