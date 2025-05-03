using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCellManager : MonoBehaviour
{
    public GameObject diamondObject;

    void Start()
    {
        // Set globalDiamond để dùng chung
        globalDiamond = diamondObject;
        globalDiamond.GetComponent<Image>().enabled = false;

        // Tìm tất cả ô có tag "GridCell"
        GameObject[] cells = GameObject.FindGameObjectsWithTag("GridCell");

        foreach (GameObject cell in cells)
        {
            // Thêm EventTrigger nếu chưa có
            EventTrigger trigger = cell.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = cell.AddComponent<EventTrigger>();

            // Tạo và thêm sự kiện PointerEnter
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((eventData) => { OnCellPointerEnter(cell); });
            trigger.triggers.Add(enterEntry);

            // Tạo và thêm sự kiện PointerExit
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((eventData) => { OnCellPointerExit(); });
            trigger.triggers.Add(exitEntry);
        }
    }

    public static GameObject globalDiamond;

    void OnCellPointerEnter(GameObject cell)
    {
        // Di chuyển Diamond vào ô này
        globalDiamond.transform.SetParent(cell.transform);
        globalDiamond.transform.localPosition = Vector3.zero;
        globalDiamond.GetComponent<Image>().enabled = true;
    }

    void OnCellPointerExit()
    {
        // Ẩn Diamond khi rời ô
        globalDiamond.GetComponent<Image>().enabled = false;
    }
}
