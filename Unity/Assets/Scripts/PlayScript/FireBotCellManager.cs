using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class FireBotManager : MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject fireBotPanel;
    public GameObject botFirePanel;

    public static GameObject globalDiamond;
    public static BotShot[] globalBotShots;  // Static để BotFireManager đọc được

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

            string gameId = PrefsHelper.GetString("gameId");
            string playerId = PrefsHelper.GetString("playerId");
            string apiURL = "http://localhost:3000/api/gameplay/fire-ship/bot";

            ShotRequest shotRequest = new ShotRequest(gameId, playerId, cell.name);
            UnityWebRequest request = CreatePostRequest(apiURL, shotRequest);

            yield return request.SendWebRequest();

            string shotType = "miss"; // default

            if (request.result == UnityWebRequest.Result.Success)
            {
                ShotResponse response = JsonUtility.FromJson<ShotResponse>(request.downloadHandler.text);
                if (response != null && response.playerShot != null)
                {
                    shotType = response.playerShot.result;

                    if (shotType == "miss")
                    {
                        BotFireManager.botShotsData.Clear();
                        BotFireManager.botShotsData.AddRange(response.botShots);
                        Debug.Log("Dữ liệu botShots đã được cập nhật.");
                        foreach (BotShot shot in BotFireManager.botShotsData)
                        {
                            Debug.Log($"BotShot Position: {shot.position}, Result: {shot.result}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Error: playerShot not found in response.");
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
                OpenBotFirePanel();
            }
        }
    }

    void OpenBotFirePanel()
    {
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

// Cấu trúc các class JSON

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
