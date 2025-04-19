using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
public class PlaceShip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false; // Cho phép raycast đi qua để bắt Cell
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / transform.root.GetComponent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Raycast để tìm xem có đối tượng nào dưới con trỏ không
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        bool placed = false;

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag("GridCell")) // Tag đặt cho cell prefab
            {
                // Snap tàu vào đúng vị trí ô cell
                rectTransform.position = result.gameObject.transform.position;
                placed = true;
                break;
            }
        }

        if (!placed)
        {
            // Nếu không có ô hợp lệ thì quay về vị trí ban đầu
            rectTransform.position = originalPosition;
        }
    }
}
