using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class FireBotManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject fireBotPanel;
    public GameObject botFirePanel;

    public static GameObject globalDiamond;
    public static List<BotShot> globalBotShots = new List<BotShot>();  // Sử dụng List thay vì array và khởi tạo

    public GameObject changeTurnPanel; // Gán trong Inspector

    void Start()
    {
        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

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

    void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    void OnCellPointerEnter(GameObject cell)
    {
        if (cell.GetComponent<GridCellStatus>().isClicked)
            return;

        globalDiamond.transform.SetParent(cell.transform);
        globalDiamond.transform.localPosition = Vector3.zero;
        globalDiamond.GetComponent<Image>().enabled = true;
    }

    void OnCellPointerExit()
    {
        globalDiamond.GetComponent<Image>().enabled = false;
    }

    void OnCellPointerClick(GameObject cell)
    {
        GridCellStatus status = cell.GetComponent<GridCellStatus>();
        if (status != null && status.isClicked)
            return;

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

            string gameId = PrefsHelper.GetInt("gameId").ToString();
            string playerId = PrefsHelper.GetInt("playerId").ToString();
            string apiURL = "http://localhost:3000/api/gameplay/fire-ship/bot";

            ShotRequest shotRequest = new ShotRequest(gameId, playerId, cell.name);
            UnityWebRequest request = CreatePostRequest(apiURL, shotRequest);

            Debug.Log($"Gửi request đến API với gameId: {gameId}, playerId: {playerId}, position: {cell.name}");
            yield return request.SendWebRequest();

            string shotType = "miss"; // default
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
                            Debug.Log($"playerShot: position={response.playerShot.position}, result={response.playerShot.result}");

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

            GameObject circlePrefab = (shotType == "hit") ? circleRedObject : circleWhiteObject;
            if (circlePrefab != null)
            {
                GameObject newCircle = Instantiate(circlePrefab, parent);
                newCircle.name = "Circle";
                newCircle.GetComponent<Image>().enabled = true;
                newCircle.transform.localPosition = pos;

                yield return new WaitForSeconds(0.5f);
            }

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
        }
    }

    IEnumerator OpenBotFirePanel()
    {
        Debug.Log("OpenBotFirePanel() được gọi");

        // Bước 1: Hiện panel hiệu ứng với scale
        changeTurnPanel.SetActive(true);
        changeTurnPanel.transform.localScale = Vector3.zero;

        // Scale từ 0 → 1 với hiệu ứng bật nảy
        LeanTween.scale(changeTurnPanel, Vector3.one, 0.4f).setEaseOutBack();

        // Chờ trong thời gian delay (ví dụ: 2 giây)
        yield return new WaitForSeconds(1.2f);

        // Ẩn panel
        changeTurnPanel.SetActive(false);

        // Bước 2: Gọi lại BotFireManager để chắc chắn có dữ liệu
        //BotFireManager botFireManager = botFirePanel.GetComponent<BotFireManager>();
        //if (botFireManager != null)
        //{
        //    botFireManager.SetBotShotsData(globalBotShots);
        //    Debug.Log($"Truyền trực tiếp {globalBotShots.Count} shots cho BotFireManager trước khi chuyển panel");
        //}

        // Bước 3: Chuyển panel
        fireBotPanel.SetActive(false);
        botFirePanel.SetActive(true);

        // Bước 4: Gọi StartBotFire
        //if (botFireManager != null)
        //{
        //    Debug.Log("Gọi StartBotFire() ngay lập tức từ OpenBotFirePanel");
        //    botFireManager.StartBotFire();
        //}
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
public class PlayerShot
{
    public string position;
    public string result;
    public SunkShip sunkShip;
    public string gameResult;
}

[System.Serializable]
public class ShotResponse
{
    public string message;
    public PlayerShot playerShot;
    public BotShot[] botShots;
}

[System.Serializable]
public class SunkShip
{
    public int shipId;
    public string shipType;
}

[System.Serializable]
public class BotShot
{
    public string position;
    public string result;
    public SunkShip sunkShip;
    public string gameResult;

    // Mặc định constructor
    public BotShot()
    {
        position = "";
        result = "";
        sunkShip = null;
        gameResult = null;
    }
}

[System.Serializable]
public class ShotRequest
{
    public string gameId;
    public string playerId;
    public string position;

    public ShotRequest(string gameId, string playerId, string position)
    {
        this.gameId = gameId;
        this.playerId = playerId;
        this.position = position;
    }
}

public class GridCellStatus : MonoBehaviour
{
    public bool isClicked = false;
}
