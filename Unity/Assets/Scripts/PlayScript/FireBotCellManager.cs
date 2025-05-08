using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class FireBotManager: MonoBehaviour
{
    public GameObject diamondObject;
    public GameObject rectangleObject;
    public GameObject circleWhiteObject;
    public GameObject circleRedObject;
    public GameObject fireBotPanel;
    public GameObject botFirePanel;

    public static GameObject globalDiamond;

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

            string shotType = "miss";
            string apiURL = "http://localhost:3000/api/gameplay/fire-ship";

            ShotRequest shotRequest = new ShotRequest(gameId, playerId, cell.name);
            UnityWebRequest request = CreatePostRequest(apiURL, shotRequest);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ShotResponse response = JsonUtility.FromJson<ShotResponse>(request.downloadHandler.text);
                shotType = response.result;
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

                // Đợi 0.5s để hiển thị hình tròn trước khi qua BotFirePanel
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
        if (fireBotPanel != null)
            fireBotPanel.SetActive(false);

        if (botFirePanel != null)
            botFirePanel.SetActive(true);
        else
            Debug.LogError("BotFirePanel not assigned in Inspector.");
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

public class GridCellStatus : MonoBehaviour
{
    public bool isClicked = false;
}

[System.Serializable]
public class ShotResponse
{
    public string position;
    public string result;
    public string sunkShip;
    public string gameResult;

    public ShotResponse(string position, string result, string sunkShip, string gameResult)
    {
        this.position = position;
        this.result = result;
        this.sunkShip = sunkShip;
        this.gameResult = gameResult;
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
