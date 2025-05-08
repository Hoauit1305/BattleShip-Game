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

    void OnEnable()
    {
        StartBotFire();
    }

    public void StartBotFire()
    {
        if (botShotsData != null && botShotsData.Count > 0)
        {
            Debug.Log("Dữ liệu botShots đã nhận được:");
            foreach (BotShot shot in botShotsData)
            {
                Debug.Log($"BotShot Position: {shot.position}, Result: {shot.result}");
            }
            StartCoroutine(ProcessBotShots());
        }
        else
        {
            Debug.Log("Không có dữ liệu botShots để xử lý.");
        }
    }

    IEnumerator ProcessBotShots()
    {
        Debug.Log("Bắt đầu xử lý botShots...");
        foreach (BotShot shot in botShotsData)
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
    }
}
