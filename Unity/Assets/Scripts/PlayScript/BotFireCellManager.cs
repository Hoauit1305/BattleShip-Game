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

    public static List<BotShot> botShotsData = new List<BotShot>();
    private List<BotShot> currentBotShots = new List<BotShot>();
    private bool dataWasSet = false;

    void OnEnable()
    {
        Debug.Log("BotFireManager - OnEnable() được gọi");

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
        foreach (BotShot shot in shots)
        {
            Debug.Log("Bot bắn tại ô: " + shot.position + " | Kết quả: " + shot.result);

            GameObject cell = GameObject.Find(shot.position);
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

            GameObject circlePrefab = (shot.result == "hit") ? circleRedObject : circleWhiteObject;
            GameObject circle = Instantiate(circlePrefab, cell.transform);
            circle.GetComponent<Image>().enabled = true;
            circle.transform.localPosition = Vector3.zero;
            yield return new WaitForSeconds(0.5f);
        }

        // Sau khi xử lý xong: hiện panel đổi lượt
        yield return StartCoroutine(ShowChangeTurnPanel());

        // Chuyển sang panel người chơi
        if (botFirePanel != null)
            botFirePanel.SetActive(false);
        if (fireBotPanel != null)
            fireBotPanel.SetActive(true);

        // Dọn dẹp dữ liệu
        currentBotShots.Clear();
        botShotsData.Clear();
        FireBotManager.globalBotShots.Clear();
        dataWasSet = false;
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
