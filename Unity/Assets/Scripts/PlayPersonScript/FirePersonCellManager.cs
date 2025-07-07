using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using WebSocketSharp;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class FirePersonManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject fireBotPanel;
    public GameObject botFirePanel;

    // Thêm hai panel mới cho kết quả trò chơi
    public GameObject winGamePanel;
    public GameObject loseGamePanel;

    // Thêm các prefab tàu
    public GameObject ship2Prefab;
    public GameObject ship31Prefab;
    public GameObject ship32Prefab;
    public GameObject ship4Prefab;
    public GameObject ship5Prefab;

    // Dictionary để theo dõi các tàu đã được đặt trên bảng
    private Dictionary<string, GameObject> placedShips = new Dictionary<string, GameObject>();

    public static GameObject globalDiamond;
    public static List<BotShot> globalBotShots = new List<BotShot>();
    public static bool isPlayerTurn = true;
    public GameObject changeTurnPanel;
    //Hightlight
    private List<GameObject> frameObjects = new List<GameObject>();
    public Color FrameColor = Color.red;

    public WebSocket socket; // socket truyền vào từ lúc tạo phòng

    void Start()
    {
       

        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

        // Tắt hình ảnh của tất cả các prefab tàu khi khởi tạo
        DisableAllShipPrefabImages();

        // Đảm bảo các panel kết quả trò chơi bị ẩn khi bắt đầu
        if (winGamePanel != null) winGamePanel.SetActive(false);
        if (loseGamePanel != null) loseGamePanel.SetActive(false);

        GameObject[] cells = GameObject.FindGameObjectsWithTag("GridCell");
        foreach (GameObject cell in cells)
        {
            if (cell.GetComponent<GridCellStatus>() == null)
                cell.AddComponent<GridCellStatus>();

            EventTrigger trigger = cell.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cell.AddComponent<EventTrigger>();

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, (eventData) => { OnCellPointerEnter(cell); });
            AddEventTrigger(trigger, EventTriggerType.PointerExit, (eventData) => { OnCellPointerExit(); });
            AddEventTrigger(trigger, EventTriggerType.PointerClick, (eventData) => { OnCellPointerClick(cell); });
        }
    }

    // Hàm tắt hình ảnh của tất cả các prefab tàu
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
        if (cell.GetComponent<GridCellStatus>().isClicked)
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

        GridCellStatus status = cell.GetComponent<GridCellStatus>();
        if (status != null && status.isClicked)
            return;

        isPlayerTurn = false;
        FireAudioManager.Instance?.PlayFireSound();
        if (status != null)
            status.isClicked = true;

        if (globalDiamond != null)
            globalDiamond.GetComponent<Image>().enabled = false;

        Transform existingRect = cell.transform.Find("Rectangle(Clone)");
        if (existingRect == null)
        {
            GameObject newRect = Instantiate(rectangleObject, cell.transform);
            newRect.name = "Rectangle";
            newRect.GetComponent<Image>().enabled = true;
            newRect.transform.localPosition = Vector3.zero;

            StartCoroutine(ChangeToCircleAndCallAPI(newRect, cell));
        }
        else
        {
            existingRect.GetComponent<Image>().enabled = true;
            StartCoroutine(ChangeToCircleAndCallAPI(existingRect.gameObject, cell));
        }
    }

    IEnumerator ChangeToCircleAndCallAPI(GameObject rectObj, GameObject cell)
    {
        yield return new WaitForSeconds(0.3f);

        if (rectObj != null)
        {
            Transform parent = rectObj.transform.parent;
            Vector3 pos = rectObj.transform.localPosition;

            Destroy(rectObj);

            // Gửi socket bắn sang server
            string gameId = PrefsHelper.GetInt("gameId").ToString();
            string playerId = PrefsHelper.GetInt("playerId").ToString();
            string position = cell.name;
            string apiURL = "https://battleship-game-production.up.railway.app/api/gameplay/fire-ship/person";

            ShotRequest shotRequest = new ShotRequest(gameId, playerId, cell.name);
            UnityWebRequest request = CreatePostRequest(apiURL, shotRequest);

            Debug.Log($"Gửi request đến API với gameId: {gameId}, playerId: {playerId}, position: {cell.name}");
            yield return request.SendWebRequest();

            string shotType = "miss"; // default
            SunkShip sunkShipData = null;
            GameResult gameResultData = null; // Biến lưu kết quả trò chơi

            Debug.Log($"API response status: {request.result}, responseCode: {request.responseCode}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Log response nguyên bản để debug
                string responseText = request.downloadHandler.text;
                Debug.Log($"API response text: {responseText}");

                try
                {
                    ShotResponse response = JsonUtility.FromJson<ShotResponse>(responseText);

                    if (response != null)
                    {
                        Debug.Log($"Parsed response - message: {response.message}");

                        if (response.playerShot != null)
                        {
                            shotType = response.playerShot.result;
                            sunkShipData = response.playerShot.sunkShip;
                            gameResultData = response.playerShot.gameResult; // Lấy kết quả trò chơi từ response

                            Debug.Log($"playerShot: position={response.playerShot.position}, result={response.playerShot.result}");
                            // Log thông tin gameResult
                            if (gameResultData != null)
                            {
                                Debug.Log($"Game result: status={gameResultData.status}, winnerId={gameResultData.winnerId}");
                            }
                            else
                            {
                                Debug.Log("Game result is null - game still in progress");
                            }
                            if (sunkShipData != null)
                            {
                                Debug.Log($"SunkShip: id={sunkShipData.shipId}, type={sunkShipData.shipType}");
                                if (sunkShipData.positions != null)
                                {
                                    Debug.Log($"Ship positions: {string.Join(", ", sunkShipData.positions)}");
                                }
                            }

                            if (shotType == "miss")
                            {
                                Debug.Log($"botShots array: {(response.botShots != null ? response.botShots.Length : 0)} items");

                                // Kiểm tra null và độ dài
                                if (response.botShots == null || response.botShots.Length == 0)
                                {
                                    Debug.LogError("API trả về response.botShots là null hoặc rỗng!");

                                    // Tạo dữ liệu mẫu để kiểm tra luồng dữ liệu
                                    Debug.Log("Tạo dữ liệu botShots mẫu để kiểm tra luồng...");
                                    BotShot testShot = new BotShot();
                                    testShot.position = "A1";
                                    testShot.result = "miss";

                                    // Thêm vào danh sách
                                    globalBotShots.Clear();
                                    globalBotShots.Add(testShot);

                                    // Đảm bảo BotFireManager có dữ liệu
                                    BotFireManager.botShotsData.Clear();
                                    BotFireManager.botShotsData.Add(testShot);
                                }
                                else
                                {
                                    // Cập nhật cả globalBotShots và botShotData của BotFireManager
                                    globalBotShots.Clear();
                                    BotFireManager.botShotsData.Clear();

                                    // Sao chép dữ liệu từ response
                                    globalBotShots.AddRange(response.botShots);
                                    BotFireManager.botShotsData.AddRange(response.botShots);

                                    // Log chi tiết
                                    Debug.Log($"Dữ liệu botShots đã được cập nhật: {response.botShots.Length} shots");
                                    for (int i = 0; i < response.botShots.Length; i++)
                                    {
                                        BotShot shot = response.botShots[i];
                                        Debug.Log($"Shot {i + 1}: Position: {shot.position}, Result: {shot.result}");
                                        // Kiểm tra xem bot có thắng không
                                        if (shot.gameResult != null && shot.gameResult.status == "completed")
                                        {
                                            Debug.Log($"Bot thắng! winnerId: {shot.gameResult.winnerId}");
                                            int currentPlayerId = PrefsHelper.GetInt("playerId");
                                            ShowGameResultPanel(shot.gameResult.winnerId.ToString() == currentPlayerId.ToString());
                                            yield break;
                                        }
                                    }
                                }

                                // Kiểm tra dữ liệu sau khi cập nhật
                                Debug.Log($"Sau khi cập nhật - globalBotShots: {globalBotShots.Count}, BotFireManager.botShotsData: {BotFireManager.botShotsData.Count}");
                            }
                        }
                        else
                        {
                            Debug.LogError("Error: playerShot is null in response");
                        }
                        if (socket != null && socket.IsAlive)
                        {
                            // Tạo JSON gửi socket
                            var fireResult = new
                            {
                                position = response.playerShot.position,
                                result = response.playerShot.result,
                                sunkShip = response.playerShot.sunkShip, // có thể null
                                gameResult = response.playerShot.gameResult // có thể null
                            };

                            string jsonFireResult = JsonUtility.ToJson(fireResult);
                            Debug.Log($"[SOCKET] Gửi fire-result: {jsonFireResult}");

                            // Emit socket fire-result (tùy SocketSharp hay IO client mà .Send hoặc Emit)
                            socket.Send(jsonFireResult);
                        }
                        else
                        {
                            Debug.LogWarning("[SOCKET] Socket null hoặc chưa connect");
                        }

                    }
                    else
                    {
                        Debug.LogError("Error: response is null after parsing");
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

            // Kiểm tra nếu có tàu bị chìm
            if (sunkShipData != null && sunkShipData.positions != null && sunkShipData.positions.Length > 0)
            {
                // Hiển thị frame highlight cho tất cả các ô của tàu bị chìm
                ShowSunkShipHighlights(sunkShipData.positions);
                // Hiển thị hình ảnh tàu dựa trên shipType và vị trí
                ShowSunkShip(sunkShipData);
            }
            else
            {
                // Hiển thị hình tròn bình thường (trắng hoặc đỏ)
                GameObject circlePrefab = (shotType == "hit") ? circleRedObject : circleWhiteObject;
                if (circlePrefab != null)
                {
                    GameObject newCircle = Instantiate(circlePrefab, parent);
                    newCircle.name = "Circle";
                    newCircle.GetComponent<Image>().enabled = true;
                    newCircle.transform.localPosition = pos;
                }
            }

            // Kiểm tra kết quả trò chơi
            if (gameResultData != null && gameResultData.status == "completed")
            {
                // Lấy ID của người chơi hiện tại
                int currentPlayerId = PrefsHelper.GetInt("playerId");
                Debug.Log($"Kiểm tra kết quả trò chơi: status={gameResultData.status}, winnerId={gameResultData.winnerId}, currentPlayerId={currentPlayerId}");

                // So sánh kết quả trò chơi với ID người chơi
                bool isWinner = gameResultData.winnerId.ToString() == currentPlayerId.ToString();
                // Hiển thị panel kết quả phù hợp
                Debug.Log(isWinner ? "Người chơi thắng!" : "Người chơi thua!");
                yield return new WaitForSeconds(0.7f);
                ShowGameResultPanel(isWinner);
                yield break; // Kết thúc luồng nếu trò chơi đã kết thúc
            }

            yield return new WaitForSeconds(0.5f);

            if (shotType == "miss")
            {
                // Tìm component BotFireManager trước khi chuyển đổi panel
                BotFireManager botFireManager = botFirePanel.GetComponent<BotFireManager>();
                if (botFireManager != null)
                {
                    Debug.Log("Tìm thấy BotFireManager component, gọi SetBotShotsData");
                    botFireManager.SetBotShotsData(globalBotShots);
                }
                else
                {
                    Debug.LogError("Không tìm thấy BotFireManager component trên botFirePanel!");
                }

                // Đảm bảo có dữ liệu trước khi chuyển panel
                if (globalBotShots.Count > 0 && BotFireManager.botShotsData.Count > 0)
                {
                    Debug.Log($"Trước khi chuyển panel: {globalBotShots.Count} shots sẵn sàng");
                    StartCoroutine(OpenBotFirePanel());
                }
                else
                {
                    Debug.LogError("Không có dữ liệu botShots mặc dù trạng thái là miss!");
                }
            }
            else
            {
                isPlayerTurn = true; // Nếu là hit → cho tiếp tục bắn
            }
        }
    }

    // Hiển thị panel kết quả trò chơi
    void ShowGameResultPanel(bool isWin)
    {
        // Đảm bảo rằng các panel kết quả trò chơi đã được gán
        if (winGamePanel == null || loseGamePanel == null)
        {
            Debug.LogError("Game result panels have not been assigned in the inspector!");
            return;
        }

        // Ẩn các panel khác
        fireBotPanel.SetActive(false);
        botFirePanel.SetActive(false);
        changeTurnPanel.SetActive(false);

        if (isWin)
        {
            // Hiển thị panel thắng
            winGamePanel.SetActive(true);
            loseGamePanel.SetActive(false);

            // Thêm hiệu ứng animation nếu cần
            winGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(winGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }
        else
        {
            // Hiển thị panel thua
            loseGamePanel.SetActive(true);
            winGamePanel.SetActive(false);

            // Thêm hiệu ứng animation nếu cần
            loseGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(loseGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }

        Debug.Log($"Hiển thị panel kết quả trò chơi: {(isWin ? "Thắng" : "Thua")}");
    }
    void ShowSunkShipHighlights(string[] positions)
    {
        // Use fireBotPanel instead of root canvas
        Transform parentTransform = fireBotPanel.transform;

        foreach (string positionName in positions)
        {
            GameObject cell = GameObject.Find(positionName);
            if (cell != null)
            {
                // Tạo frame highlight cho mỗi ô
                GameObject frame = new GameObject("FireBotHighlight");
                RectTransform frameTransform = frame.AddComponent<RectTransform>();
                frame.transform.SetParent(parentTransform);

                // Thiết lập vị trí và kích thước
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    frameTransform.position = cell.transform.position;
                    frameTransform.sizeDelta = cellRect.sizeDelta;
                    frameTransform.localScale = Vector3.one;

                    // Thêm component Image và thiết lập màu
                    Image frameImage = frame.AddComponent<Image>();
                    frameImage.color = new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.4f); // Màu đỏ semi-transparent

                    // Thêm vào danh sách để có thể xóa sau này
                    frameObjects.Add(frame);

                    Debug.Log($"Đã tạo highlight cho ô {positionName}");
                }
            }
        }
    }
    // Hàm hiển thị tàu bị chìm
    void ShowSunkShip(SunkShip sunkShip)
    {
        Debug.Log($"Hiển thị tàu chìm: {sunkShip.shipType} tại {string.Join(", ", sunkShip.positions)}");

        // Xác định prefab tàu dựa trên shipType
        GameObject shipPrefab = GetShipPrefabByType(sunkShip.shipType);

        if (shipPrefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho tàu: {sunkShip.shipType}");
            return;
        }

        // Kiểm tra xem tàu này đã được đặt trước đó chưa
        string shipKey = sunkShip.shipId.ToString();
        GameObject shipInstance;

        if (placedShips.ContainsKey(shipKey))
        {
            // Nếu tàu đã tồn tại, di chuyển nó đến vị trí mới
            shipInstance = placedShips[shipKey];
            Debug.Log($"Tàu {shipKey} đã tồn tại, di chuyển đến vị trí mới");
        }
        else
        {
            // Lấy ô đầu tiên để đặt tàu
            GameObject firstCell = GameObject.Find(sunkShip.positions[0]);
            if (firstCell == null)
            {
                Debug.LogError($"Không tìm thấy ô: {sunkShip.positions[0]}");
                return;
            }

            // Tạo instance mới của tàu
            shipInstance = Instantiate(shipPrefab, firstCell.transform);
            shipInstance.name = sunkShip.shipType;

            // Lưu vào dictionary để tái sử dụng sau này
            placedShips.Add(shipKey, shipInstance);
            Debug.Log($"Tàu mới {shipKey} đã được tạo");
        }

        // Xóa các vòng tròn đỏ tại các vị trí của tàu
        foreach (string positionName in sunkShip.positions)
        {
            GameObject cell = GameObject.Find(positionName);
            if (cell != null)
            {
                // Xóa các circle hiện tại trên cell này
                Transform circleTransform = cell.transform.Find("Circle");
                if (circleTransform != null)
                {
                    Destroy(circleTransform.gameObject);
                }
            }
        }

        // Đặt tàu vào vị trí đầu tiên
        GameObject startCell = GameObject.Find(sunkShip.positions[0]);
        if (startCell != null)
        {
            // Di chuyển tàu đến ô đầu tiên
            shipInstance.transform.SetParent(startCell.transform);
            shipInstance.transform.localPosition = Vector3.zero;

            // Xác định hướng tàu
            bool isVertical = IsShipVertical(sunkShip.positions);

            // Cấu hình hình ảnh và kích thước của tàu
            ConfigureShipVisual(shipInstance, sunkShip, isVertical);

            // Bật (enable) hình ảnh của tàu
            Image shipImage = shipInstance.GetComponent<Image>();
            if (shipImage != null)
            {
                shipImage.enabled = true;
                Debug.Log($"Đã bật hình ảnh cho tàu {sunkShip.shipType}");
            }
            else
            {
                Debug.LogError($"Không tìm thấy component Image trên tàu {sunkShip.shipType}");
            }
        }
        else
        {
            Debug.LogError($"Không tìm thấy ô bắt đầu: {sunkShip.positions[0]}");
        }
    }

    // Xác định tàu đặt theo chiều dọc hay ngang
    bool IsShipVertical(string[] positions)
    {
        if (positions.Length <= 1) return false;

        // Lấy ký tự đầu tiên (chữ cái) của vị trí đầu tiên và thứ hai
        char firstLetter = positions[0][0];
        char secondLetter = positions[1][0];

        // Nếu chữ cái khác nhau => tàu đặt theo chiều dọc
        return firstLetter != secondLetter;
    }

    // Lấy prefab tàu dựa vào loại
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

    // Thiết lập hiển thị cho tàu (kích thước, hướng)
    void ConfigureShipVisual(GameObject shipObject, SunkShip sunkShip, bool isVertical)
    {
        RectTransform rectTransform = shipObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Lấy kích thước của một ô
        GameObject firstCell = GameObject.Find(sunkShip.positions[0]);
        RectTransform cellRect = firstCell?.GetComponent<RectTransform>();
        if (cellRect == null) return;
        float cellSize = cellRect.rect.width;

        // Số ô mà tàu chiếm
        int shipSize = GetShipSizeFromType(sunkShip.shipType);

        // Reset rotation và scale trước khi điều chỉnh
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        // Giảm kích thước để tránh dính lưới
        float scaleFactor = 0.9f;

        // Thiết lập kích thước tàu ngang (cơ bản)
        rectTransform.sizeDelta = new Vector2(cellSize * shipSize * scaleFactor, cellSize * scaleFactor);

        // Nếu là tàu dọc thì xoay 90 độ
        if (isVertical)
        {
            rectTransform.Rotate(0, 0, 90);
        }

        // Lấy các ô đầu và cuối để xác định vị trí chính xác
        string firstCellName = sunkShip.positions[0];
        string lastCellName = sunkShip.positions[sunkShip.positions.Length - 1];

        GameObject firstCellObj = GameObject.Find(firstCellName);
        GameObject lastCellObj = GameObject.Find(lastCellName);

        if (firstCellObj != null && lastCellObj != null)
        {
            Vector3 firstPos = firstCellObj.transform.position;
            Vector3 lastPos = lastCellObj.transform.position;

            // Xác định vị trí trung tâm của tàu
            Vector3 centerPos = (firstPos + lastPos) / 2f;

            // Di chuyển tàu đến vị trí trung tâm
            rectTransform.position = centerPos;
        }

        // Đảm bảo tàu hiển thị phía trên các phần tử khác
        shipObject.transform.SetAsLastSibling();

        // Đảm bảo hình ảnh của tàu được hiển thị rõ ràng
        Image shipImage = shipObject.GetComponent<Image>();
        if (shipImage != null)
        {
            shipImage.raycastTarget = false;

            if (shipImage is UnityEngine.UI.Image)
            {
                UnityEngine.UI.Image uiImage = shipImage as UnityEngine.UI.Image;
                if (uiImage.type == UnityEngine.UI.Image.Type.Sliced)
                {
                    uiImage.pixelsPerUnitMultiplier = 1;
                }
            }
        }
    }

    // Lấy kích thước tàu từ loại tàu
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

    IEnumerator OpenBotFirePanel()
    {
        Debug.Log("OpenBotFirePanel() được gọi");

        changeTurnPanel.SetActive(true);
        changeTurnPanel.transform.localScale = Vector3.zero;

        LeanTween.scale(changeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();

        yield return new WaitForSeconds(1.2f);

        changeTurnPanel.SetActive(false);
        fireBotPanel.SetActive(false);
        botFirePanel.SetActive(true);
    }

    UnityWebRequest CreatePostRequest(string url, ShotRequest shotRequest)
    {
        string jsonBody = JsonUtility.ToJson(shotRequest);
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        return request;
    }
}

// Cấu trúc các class JSON - Sửa lại để phù hợp với cấu trúc phản hồi từ API
[System.Serializable]
public class GameResultPerson
{
    public string status;
    public int winnerId;
}

[System.Serializable]
public class PlayerPersonShot
{
    public string position;
    public string result;
    public SunkShip sunkShip;
    public GameResult gameResult;
}

[System.Serializable]
public class ShotPersonResponse
{
    public string message;
    public PlayerShot playerShot;
    public BotShot[] botShots;
}

[System.Serializable]
public class SunkShipPerson
{
    public int shipId;
    public string shipType;
    public string[] positions;
}

[System.Serializable]
public class ShotPersonRequest
{
    public string gameId;
    public string playerId;
    public string position;

    public ShotPersonRequest(string gameId, string playerId, string position)
    {
        this.gameId = gameId;
        this.playerId = playerId;
        this.position = position;
    }   
}

public class GridCellPersonStatus : MonoBehaviour
{
    public bool isClicked = false;
}