using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BotFireManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject botFirePanel;
    public GameObject fireBotPanel;
    public GameObject changeTurnPanel;

    // Thêm các panel kết quả trò chơi
    public GameObject winGamePanel;
    public GameObject loseGamePanel;

    // Thêm prefab tàu cho hiển thị tàu bị chìm
    public GameObject ship2Prefab;
    public GameObject ship31Prefab;
    public GameObject ship32Prefab;
    public GameObject ship4Prefab;
    public GameObject ship5Prefab;

    //Thêm âm thanh khi bắn
    public AudioClip fireSound;
    private AudioSource audioSource;


    // Dictionary để theo dõi các tàu đã được đặt
    private Dictionary<string, GameObject> placedShips = new Dictionary<string, GameObject>();

    public static List<BotShot> botShotsData = new List<BotShot>();
    private List<BotShot> currentBotShots = new List<BotShot>();
    private bool dataWasSet = false;
    
    //Hightlight
    private List<GameObject> frameObjects = new List<GameObject>();
    public Color FrameColor = Color.red;
    void OnEnable()
    {
        Debug.Log("BotFireManager - OnEnable() được gọi");

        // Đảm bảo các panel kết quả trò chơi bị ẩn khi bắt đầu
        if (winGamePanel != null) winGamePanel.SetActive(false);
        if (loseGamePanel != null) loseGamePanel.SetActive(false);

        if (currentBotShots.Count > 0 || botShotsData.Count > 0)
        {
            Debug.Log($"OnEnable - có dữ liệu sẵn sàng: currentBotShots={currentBotShots.Count}, botShotsData={botShotsData.Count}");
            StartBotFire();
        }
        else
        {
            Debug.Log("OnEnable - không có dữ liệu, sử dụng coroutine");
            StartCoroutine(DelayedStartBotFire());
        }
    }

    IEnumerator DelayedStartBotFire()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            Debug.Log($"DelayedStartBotFire - Frame {i + 1}: kiểm tra dữ liệu");

            if (currentBotShots.Count > 0 || botShotsData.Count > 0)
            {
                Debug.Log("Tìm thấy dữ liệu, bắt đầu xử lý");
                break;
            }
        }

        Debug.Log($"DelayedStartBotFire - BotShotsData count: {botShotsData.Count}, currentBotShots: {currentBotShots.Count}, dataWasSet: {dataWasSet}");
        StartBotFire();
    }

    public void SetBotShotsData(List<BotShot> data)
    {
        if (data != null && data.Count > 0)
        {
            currentBotShots.Clear();
            foreach (BotShot shot in data)
            {
                BotShot copy = new BotShot
                {
                    position = shot.position,
                    result = shot.result,
                    sunkShip = shot.sunkShip,
                    gameResult = shot.gameResult
                };
                currentBotShots.Add(copy);
            }

            botShotsData.Clear();
            foreach (BotShot shot in data)
            {
                BotShot copy = new BotShot
                {
                    position = shot.position,
                    result = shot.result,
                    sunkShip = shot.sunkShip,
                    gameResult = shot.gameResult
                };
                botShotsData.Add(copy);
            }

            dataWasSet = true;
            Debug.Log($"SetBotShotsData được gọi với {data.Count} shots");
            foreach (BotShot shot in currentBotShots)
            {
                Debug.Log($"SetBotShotsData: Position={shot.position}, Result={shot.result}");
            }
        }
        else
        {
            Debug.LogError("SetBotShotsData được gọi với dữ liệu rỗng hoặc null");
        }
    }

    public void StartBotFire()
    {
        List<BotShot> shotsToProcess = new List<BotShot>();

        if (dataWasSet && currentBotShots.Count > 0)
        {
            shotsToProcess = new List<BotShot>(currentBotShots);
            Debug.Log("Sử dụng dữ liệu botShots từ SetBotShotsData");
        }
        else if (FireBotManager.globalBotShots != null && FireBotManager.globalBotShots.Count > 0)
        {
            shotsToProcess = new List<BotShot>(FireBotManager.globalBotShots);
            Debug.Log("Sử dụng dữ liệu botShots từ FireBotManager.globalBotShots");
        }
        else if (botShotsData != null && botShotsData.Count > 0)
        {
            shotsToProcess = new List<BotShot>(botShotsData);
            Debug.Log("Sử dụng dữ liệu botShots từ botShotsData tĩnh");
        }

        Debug.Log($"DEBUG - Trạng thái dữ liệu: currentBotShots={currentBotShots.Count}, globalBotShots={FireBotManager.globalBotShots.Count}, botShotsData={botShotsData.Count}");

        if (shotsToProcess.Count > 0)
        {
            Debug.Log($"Dữ liệu botShots đã nhận được: {shotsToProcess.Count} shots");
            StartCoroutine(ProcessBotShots(shotsToProcess));
        }
        else
        {
            Debug.LogWarning("Không có dữ liệu botShots để xử lý!");
            if (fireBotPanel != null)
            {
                botFirePanel.SetActive(false);
                fireBotPanel.SetActive(true);
            }
        }
    }

    IEnumerator ProcessBotShots(List<BotShot> shots)
    {
        Debug.Log("Bắt đầu xử lý botShots...");

        // Biến để lưu kết quả trận đấu nếu có
        BotShot finalShot = null;

        foreach (BotShot shot in shots)
        {
            Debug.Log("Bot bắn tại ô: " + shot.position + " | Kết quả: " + shot.result);

            GameObject cell = GameObject.Find(shot.position);
            FireAudioManager.Instance?.PlayFireSound();
            if (cell == null)
            {
                Debug.LogError("Không tìm thấy ô " + shot.position);
                continue;
            }

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

            // Kiểm tra xem có tàu bị chìm không
            if (shot.sunkShip != null && shot.sunkShip.positions != null && shot.sunkShip.positions.Length > 0)
            {
                // Hiển thị frame highlight cho tất cả các ô của tàu bị chìm
                ShowSunkShipHighlights(shot.sunkShip.positions);
                // Hiển thị tàu bị chìm
                ShowSunkShip(shot.sunkShip);
            }
            else
            {
                // Hiển thị circle thông thường
                GameObject circlePrefab = (shot.result == "hit") ? circleRedObject : circleWhiteObject;
                GameObject circle = Instantiate(circlePrefab, cell.transform);
                circle.name = "Circle"; // Thêm dòng này để đặt tên cho circle giống với FireBotManager
                circle.GetComponent<Image>().enabled = true;
                circle.transform.localPosition = Vector3.zero;
            }

            // Lưu lại phát bắn cuối cùng để kiểm tra kết quả trò chơi
            if (shot.gameResult != null && shot.gameResult.status == "completed")
            {
                finalShot = shot;
            }

            // Chỉ đợi giữa các phát bắn nếu chưa phải phát cuối cùng
            if (shot != shots[shots.Count - 1])
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Đợi thêm 0.5 giây sau khi hiển thị con tàu cuối cùng trước khi kết thúc trò chơi
        yield return new WaitForSeconds(0.5f);

        // Kiểm tra nếu có kết quả trò chơi sau khi đã xử lý tất cả các phát bắn
        if (finalShot != null)
        {
            Debug.Log($"Kết quả trò chơi: status={finalShot.gameResult.status}, winnerId={finalShot.gameResult.winnerId}");

            // Lấy ID của người chơi hiện tại
            int currentPlayerId = PrefsHelper.GetInt("playerId");

            // Ngược lại với FireBotManager
            // Nếu winnerId KHÔNG trùng với ID người chơi => người chơi thua
            bool isWinner = finalShot.gameResult.winnerId.ToString() == currentPlayerId.ToString();

            // Hiển thị panel kết quả
            ShowGameResultPanel(isWinner);
        }
        else
        {
            // Nếu không có kết quả trò chơi (trận đấu chưa kết thúc)
            // Sau khi xử lý xong: hiện panel đổi lượt
            yield return StartCoroutine(ShowChangeTurnPanel());

            // Chuyển sang panel người chơi
            if (botFirePanel != null)
                botFirePanel.SetActive(false);
            if (fireBotPanel != null)
            {
                fireBotPanel.SetActive(true);
                FireBotManager.isPlayerTurn = true;
            }
        }

        // Dọn dẹp dữ liệu
        currentBotShots.Clear();
        botShotsData.Clear();
        FireBotManager.globalBotShots.Clear();
        dataWasSet = false;
    }

    // Hiển thị panel kết quả trò chơi
    void ShowGameResultPanel(bool isWin)
    {
        // Đảm bảo các panel kết quả đã được gán
        if (winGamePanel == null || loseGamePanel == null)
        {
            Debug.LogError("Game result panels have not been assigned in the inspector!");
            return;
        }

        // Ẩn các panel khác
        botFirePanel.SetActive(false);
        fireBotPanel.SetActive(false);
        changeTurnPanel.SetActive(false);

        if (isWin)
        {
            // Hiển thị panel thắng
            winGamePanel.SetActive(true);
            loseGamePanel.SetActive(false);

            // Thêm hiệu ứng animation
            winGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(winGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }
        else
        {
            // Hiển thị panel thua
            loseGamePanel.SetActive(true);
            winGamePanel.SetActive(false);

            // Thêm hiệu ứng animation
            loseGamePanel.transform.localScale = Vector3.zero;
            LeanTween.scale(loseGamePanel, Vector3.one, 0.5f).setEaseOutBack();
        }

        Debug.Log($"Hiển thị panel kết quả trò chơi: {(isWin ? "Thắng" : "Thua")}");
    }
    //Hight light ô tàu
    void ShowSunkShipHighlights(string[] positions)
    {
        // Use botFirePanel instead of root canvas
        Transform parentTransform = botFirePanel.transform;

        foreach (string positionName in positions)
        {
            GameObject cell = GameObject.Find(positionName);
            if (cell != null)
            {
                // Tạo frame highlight cho mỗi ô
                GameObject frame = new GameObject("BotFireHighlight");
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
    // Hiển thị tàu bị chìm
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

        // Kiểm tra xem tàu đã được đặt trước đó chưa
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

    IEnumerator ShowChangeTurnPanel()
    {
        if (changeTurnPanel != null)
        {
            changeTurnPanel.SetActive(true);
            changeTurnPanel.transform.localScale = Vector3.zero;
            LeanTween.scale(changeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();

            yield return new WaitForSeconds(1.2f);
            changeTurnPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("changeTurnPanel chưa được gán!");
            yield return null;
        }
    }
}