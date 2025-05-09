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

    public static List<BotShot> botShotsData = new List<BotShot>();
    private List<BotShot> currentBotShots = new List<BotShot>();
    private bool dataWasSet = false;

    void OnEnable()
    {
        Debug.Log("BotFireManager - OnEnable() được gọi");
        // Cố gắng khởi động trò chơi ngay lập tức thay vì sử dụng coroutine
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
        // Đợi một vài frame để đảm bảo dữ liệu đã được truyền vào
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

        // Kiểm tra dữ liệu
        Debug.Log($"DelayedStartBotFire - BotShotsData count: {botShotsData.Count}, currentBotShots: {currentBotShots.Count}, dataWasSet: {dataWasSet}");

        StartBotFire();
    }

    // Phương thức để FireBotManager gọi trực tiếp
    public void SetBotShotsData(List<BotShot> data)
    {
        if (data != null && data.Count > 0)
        {
            currentBotShots.Clear();
            foreach (BotShot shot in data)
            {
                // Tạo một bản sao để tránh tham chiếu trực tiếp
                BotShot copy = new BotShot();
                copy.position = shot.position;
                copy.result = shot.result;
                copy.sunkShip = shot.sunkShip;
                copy.gameResult = shot.gameResult;
                currentBotShots.Add(copy);
            }

            botShotsData.Clear();
            foreach (BotShot shot in data)
            {
                // Tạo một bản sao cho botShotsData static
                BotShot copy = new BotShot();
                copy.position = shot.position;
                copy.result = shot.result;
                copy.sunkShip = shot.sunkShip;
                copy.gameResult = shot.gameResult;
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
        // Sử dụng dữ liệu từ các nguồn theo thứ tự ưu tiên
        List<BotShot> shotsToProcess = new List<BotShot>();

        // 1. Ưu tiên sử dụng dữ liệu được set thông qua SetBotShotsData
        if (dataWasSet && currentBotShots.Count > 0)
        {
            shotsToProcess = new List<BotShot>(currentBotShots);
            Debug.Log("Sử dụng dữ liệu botShots từ SetBotShotsData");
        }
        // 2. Tiếp theo, thử sử dụng dữ liệu từ FireBotManager
        else if (FireBotManager.globalBotShots != null && FireBotManager.globalBotShots.Count > 0)
        {
            shotsToProcess = new List<BotShot>(FireBotManager.globalBotShots);
            Debug.Log("Sử dụng dữ liệu botShots từ FireBotManager.globalBotShots");
        }
        // 3. Cuối cùng, sử dụng dữ liệu đã lưu trong botShotsData
        else if (botShotsData != null && botShotsData.Count > 0)
        {
            shotsToProcess = new List<BotShot>(botShotsData);
            Debug.Log("Sử dụng dữ liệu botShots từ botShotsData tĩnh");
        }

        // Log thông tin debug về các nguồn dữ liệu
        Debug.Log($"DEBUG - Trạng thái dữ liệu: currentBotShots={currentBotShots.Count}, globalBotShots={FireBotManager.globalBotShots.Count}, botShotsData={botShotsData.Count}");

        if (shotsToProcess.Count > 0)
        {
            Debug.Log($"Dữ liệu botShots đã nhận được: {shotsToProcess.Count} shots");
            foreach (BotShot shot in shotsToProcess)
            {
                Debug.Log($"BotShot Position: {shot.position}, Result: {shot.result}");
            }
            StartCoroutine(ProcessBotShots(shotsToProcess));
        }
        else
        {
            Debug.LogWarning("Không có dữ liệu botShots để xử lý!");
            // Trường hợp lỗi, có thể quay lại FireBotPanel
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

            // Hiện hình thoi
            GameObject diamond = Instantiate(diamondObject, cell.transform);
            diamond.GetComponent<Image>().enabled = true;
            diamond.transform.localPosition = Vector3.zero;
            yield return new WaitForSeconds(0.5f);
            Destroy(diamond);

            // Hiện hình chữ nhật
            GameObject rectangle = Instantiate(rectangleObject, cell.transform);
            rectangle.GetComponent<Image>().enabled = true;
            rectangle.transform.localPosition = Vector3.zero;
            yield return new WaitForSeconds(0.5f);
            Destroy(rectangle);

            // Hiện hình tròn dựa vào result
            GameObject circlePrefab = (shot.result == "hit") ? circleRedObject : circleWhiteObject;
            GameObject circle = Instantiate(circlePrefab, cell.transform);
            circle.GetComponent<Image>().enabled = true;
            circle.transform.localPosition = Vector3.zero;

            // Chờ 0.5s trước khi tới lượt tiếp
            yield return new WaitForSeconds(0.5f);
        }

        // Xong hết thì chuyển lại FireBotPanel
        if (botFirePanel != null)
            botFirePanel.SetActive(false);
        if (fireBotPanel != null)
            fireBotPanel.SetActive(true);

        // Xóa dữ liệu đã xử lý để tránh xử lý lại
        currentBotShots.Clear();
        botShotsData.Clear();
        FireBotManager.globalBotShots.Clear();
        dataWasSet = false; // Reset trạng thái
    }
}