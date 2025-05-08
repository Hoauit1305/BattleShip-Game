using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class GirdClickManager : MonoBehaviour
{
    public GameObject rectangleObject; // Prefab hình chữ nhật
    public GameObject circleObject;    // Prefab hình tròn màu trắng

    void Start()
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("GridCell");

        foreach (GameObject cell in cells)
        {
            EventTrigger trigger = cell.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cell.AddComponent<EventTrigger>();

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((eventData) => { OnCellPointerClick(cell); });
            trigger.triggers.Add(clickEntry);
        }
    }

    void OnCellPointerClick(GameObject cell)
    {
        if (GridCellManager.globalDiamond != null)
            GridCellManager.globalDiamond.GetComponent<Image>().enabled = false;

        // Nếu chưa có rectangle thì tạo
        Transform existingRect = cell.transform.Find("Rectangle(Clone)");
        if (existingRect == null)
        {
            GameObject newRect = Instantiate(rectangleObject, cell.transform);
            newRect.name = "Rectangle";
            newRect.GetComponent<Image>().enabled = true;
            newRect.transform.localPosition = Vector3.zero;

            // Chạy coroutine đổi thành Circle sau 0.3s
            StartCoroutine(ChangeToCircle(newRect));
        }
        else
        {
            existingRect.GetComponent<Image>().enabled = true;
            StartCoroutine(ChangeToCircle(existingRect.gameObject));
        }
    }

    IEnumerator ChangeToCircle(GameObject rectObj)
    {
        yield return new WaitForSeconds(0.3f);

        if (rectObj != null)
        {
            // Lưu vị trí và parent
            Transform parent = rectObj.transform.parent;
            Vector3 pos = rectObj.transform.localPosition;

            Destroy(rectObj); // Xóa rectangle

            // Tạo circle mới
            if (circleObject != null)
            {
                GameObject newCircle = Instantiate(circleObject, parent);
                newCircle.name = "Circle";
                newCircle.GetComponent<Image>().enabled = true;
                newCircle.transform.localPosition = pos;
            }
        }
    }
}
