using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class GridCellManager : MonoBehaviour
{
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    public GameObject diamondObject;
    public GameObject rectangleObject;

    private int clickCount = 0;
    private float doubleClickThreshold = 0.3f;
    private float clickTimer = 0f;

    void Start()
    {
        // Tắt hình lúc start
        diamondObject.GetComponent<Image>().enabled = false;
        rectangleObject.GetComponent<Image>().enabled = false;
    }

    void Update()
    {
        if (clickTimer > 0)
        {
            clickTimer -= Time.deltaTime;
            if (clickTimer <= 0)
            {
                clickCount = 0;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.CompareTag("GridCell"))
                {
                    clickCount++;
                    clickTimer = doubleClickThreshold;

                    if (clickCount == 1)
                    {
                        ShowDiamond(result.gameObject);
                    }
                    else if (clickCount == 2)
                    {
                        ShowRectangle(result.gameObject);
                        clickCount = 0;
                        clickTimer = 0;
                    }
                    break;
                }
            }
        }
    }

    void ShowDiamond(GameObject cell)
    {
        // Move Diamond vào ô được click
        diamondObject.transform.SetParent(cell.transform);
        diamondObject.transform.localPosition = Vector3.zero;
        diamondObject.GetComponent<Image>().enabled = true;

        // Tắt Rectangle
        rectangleObject.GetComponent<Image>().enabled = false;
    }

    void ShowRectangle(GameObject cell)
    {
        // Move Rectangle vào ô được click
        rectangleObject.transform.SetParent(cell.transform);
        rectangleObject.transform.localPosition = Vector3.zero;
        rectangleObject.GetComponent<Image>().enabled = true;

        // Tắt Diamond
        diamondObject.GetComponent<Image>().enabled = false;
    }
}
