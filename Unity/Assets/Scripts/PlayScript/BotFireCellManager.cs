using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class BotFireManager: MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject playerFirePanel;
    public GameObject fireBotPanel;
    public static GameObject globalDiamond;

    void Start()
    {
        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

        GameObject[] cells = GameObject.FindGameObjectsWithTag("BotGridCell");

        foreach (GameObject cell in cells)
        {
            if (cell.GetComponent<BotFireCellStatus>() == null)
                cell.AddComponent<BotFireCellStatus>();

            EventTrigger trigger = cell.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cell.AddComponent<EventTrigger>();

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, (eventData) => { OnCellPointerEnter(cell); });
            AddEventTrigger(trigger, EventTriggerType.PointerExit, (eventData) => { OnCellPointerExit_Handler(); });
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
        if (cell.GetComponent<BotFireCellStatus>().isClicked)
            return;

        globalDiamond.transform.SetParent(cell.transform);
        globalDiamond.transform.localPosition = Vector3.zero;
        globalDiamond.GetComponent<Image>().enabled = true;
    }

    void OnCellPointerExit_Handler()
    {
        globalDiamond.GetComponent<Image>().enabled = false;
    }

    void OnCellPointerClick(GameObject cell)
    {
        BotFireCellStatus status = cell.GetComponent<BotFireCellStatus>();
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

            StartCoroutine(ChangeToCircleAndCallBotAPI(newRect, cell));
        }
        else
        {
            existingRect.GetComponent<Image>().enabled = true;
            StartCoroutine(ChangeToCircleAndCallBotAPI(existingRect.gameObject, cell));
        }
    }

    IEnumerator ChangeToCircleAndCallBotAPI(GameObject rectObj, GameObject cell)
    {
        yield return new WaitForSeconds(0.3f);

        if (rectObj != null)
        {
            Transform parent = rectObj.transform.parent;
            Vector3 pos = rectObj.transform.localPosition;
            Destroy(rectObj);

            // 1. Mặc định kết quả là "miss"
            string playerShotResult = "miss";

            // 2. Chuẩn bị gọi API
            string gameId = PrefsHelper.GetString("gameId");
            string playerId = PrefsHelper.GetString("playerId");

            string apiURL = "http://localhost:3000/api/gameplay/fire-ship/bot";
            FireShipRequest shotRequest = new FireShipRequest(gameId, playerId, cell.name);
            UnityWebRequest request = CreatePostRequest(apiURL, shotRequest);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                BotFireShotResponse response = JsonUtility.FromJson<BotFireShotResponse>(request.downloadHandler.text);

                // 3. Lấy kết quả thực tế từ API
                playerShotResult = response.playerShot.result;

                // 4. Bot bắn
                yield return new WaitForSeconds(0.5f);

                foreach (var botShot in response.botShots)
                {
                    GameObject[] playerCells = GameObject.FindGameObjectsWithTag("GridCell");
                    foreach (GameObject playerCell in playerCells)
                    {
                        if (playerCell.name == botShot.position)
                        {
                            GameObject prefab = (botShot.result == "hit") ? circleRedObject : circleWhiteObject;
                            GameObject botCircle = Instantiate(prefab, playerCell.transform);
                            botCircle.name = "BotCircle";
                            botCircle.GetComponent<Image>().enabled = true;
                            botCircle.transform.localPosition = Vector3.zero;
                        }
                    }
                }
            }

            // 5. Tạo hình tròn dựa vào biến playerShotResult
            GameObject playerCirclePrefab = (playerShotResult == "hit") ? circleRedObject : circleWhiteObject;
            GameObject newCircle = Instantiate(playerCirclePrefab, parent);
            newCircle.name = "PlayerCircle";
            newCircle.GetComponent<Image>().enabled = true;
            newCircle.transform.localPosition = pos;

            // 6. Chuyển panel
            yield return new WaitForSeconds(0.5f);
            OpenPlayerFirePanel();
        }
    }

    void OpenPlayerFirePanel()
    {
        if (fireBotPanel != null)
            fireBotPanel.SetActive(false);

        if (playerFirePanel != null)
            playerFirePanel.SetActive(true);
    }

    UnityWebRequest CreatePostRequest(string url, object requestBody)
    {
        string jsonBody = JsonUtility.ToJson(requestBody);
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

[System.Serializable]
public class BotFireShotResponse
{
    public string message;
    public FireShotResponse playerShot;
    public List<FireShotResponse> botShots;
}

[System.Serializable]
public class FireShotResponse
{
    public string position;
    public string result;
    public string sunkShip;
    public string gameResult;
}

[System.Serializable]
public class FireShipRequest
{
    public string gameId;
    public string playerId;
    public string position;
    public FireShipRequest(string gameId, string playerId, string position)
    {
        this.gameId = gameId;
        this.playerId = playerId;
        this.position = position;
    }
}

public class BotFireCellStatus : MonoBehaviour
{
    public bool isClicked = false;
}