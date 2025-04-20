using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI; // Thêm cho việc thay đổi màu sắc

public class PlaceShip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    public int ShipLength;
    public float SizeCell;

    // Thêm tham chiếu đến GameObject của lưới
    public RectTransform gridTransform;

    // Danh sách các đối tượng khung bao quanh
    private List<GameObject> frameObjects = new List<GameObject>();

    // Màu sắc của khung
    public Color validFrameColor = Color.green;
    public Color invalidFrameColor = Color.red;

    public static List<RectTransform> placedShips = new List<RectTransform>();

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

        // Tìm ô hiện tại đang hover
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        // Xóa khung cũ
        ClearFrames();

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag("GridCell"))
            {
                Vector3 cellPosition = result.gameObject.transform.position;

                // Điều chỉnh vị trí cho tàu
                Vector3 shipPosition = cellPosition;
                if (ShipLength % 2 == 0)
                {
                    shipPosition.x -= SizeCell / 2f;
                }

                // Kiểm tra tàu có nằm trong lưới không
                bool isValid = IsShipPlacementValid(shipPosition);

                // Hiển thị khung cho tàu
                ShowShipFrame(cellPosition, isValid);

                break;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Xóa tất cả khung
        ClearFrames();

        // Raycast để tìm xem có đối tượng nào dưới con trỏ không
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        bool placed = false;

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag("GridCell"))
            {
                // Lấy vị trí cell được chọn
                Vector3 cellPosition = result.gameObject.transform.position;

                // Tính toán vị trí mới cho tàu
                Vector3 newShipPosition = cellPosition;

                // Điều chỉnh vị trí cho tàu có chiều dài chẵn
                if (ShipLength % 2 == 0)
                {
                    newShipPosition.x -= SizeCell / 2f;
                }

                // Kiểm tra xem tàu có nằm hoàn toàn trong lưới không
                if (IsShipPlacementValid(newShipPosition))
                {
                    // Nếu hợp lệ, đặt tàu vào vị trí mới
                    rectTransform.position = newShipPosition;

                    if (!placedShips.Contains(rectTransform))
                    {
                        placedShips.Add(rectTransform);
                    }

                    placed = true;
                }
            }
        }

        if (!placed)
        {
            // Nếu không có ô hợp lệ hoặc tàu nằm ngoài lưới, quay về vị trí ban đầu
            rectTransform.position = originalPosition;
        }
    }

    // Phương thức mới để kiểm tra xem tàu có nằm trong lưới không
    private bool IsShipPlacementValid(Vector3 shipPosition)
    {
        if (gridTransform == null)
        {
            Debug.LogError("Grid Transform is not assigned!");
            return false;
        }

        

        // Tính toán vị trí các cạnh của lưới trong không gian thế giới
        Vector3[] gridCorners = new Vector3[4];
        gridTransform.GetWorldCorners(gridCorners);

        float gridMinX = gridCorners[0].x;
        float gridMaxX = gridCorners[2].x;
        float gridMinY = gridCorners[0].y;
        float gridMaxY = gridCorners[2].y;

        // Tính toán kích thước của tàu
        float shipWidth = ShipLength * SizeCell;
        float shipHeight = SizeCell;

        // Tính toán vị trí các cạnh của tàu
        float shipMinX = shipPosition.x - shipWidth / 2f;
        float shipMaxX = shipPosition.x + shipWidth / 2f;
        float shipMinY = shipPosition.y - shipHeight / 2f;

        float shipMaxY = shipPosition.y + shipHeight / 2f;
        Rect shipRect = new Rect(shipMinX, shipMinY, shipWidth, shipHeight);
        // Kiểm tra xem tàu có nằm hoàn toàn trong lưới không
        if (shipMinX < gridMinX || shipMaxX > gridMaxX || shipMinY < gridMinY || shipMaxY > gridMaxY)
        {
            return false;
        }

        // 2. Kiểm tra có chồng lên tàu đã đặt không
        foreach (var placed in placedShips)
        {
            if (placed == rectTransform) continue; // bỏ qua chính nó

            Vector3 placedPos = placed.position;
            float placedWidth = placed.sizeDelta.x;
            float placedHeight = placed.sizeDelta.y;

            Rect placedRect = new Rect(
                placedPos.x - placedWidth / 2f,
                placedPos.y - placedHeight / 2f,
                placedWidth,
                placedHeight
            );

            if (shipRect.Overlaps(placedRect))
            {
                return false;
            }
        }

        return true;
    }

    private void ShowShipFrame(Vector3 startCellPosition, bool isValid)
    {
        // Tính toán vị trí trung tâm của tàu
        Vector3 shipCenter = startCellPosition;

        // Điều chỉnh vị trí nếu tàu có chiều dài chẵn
        if (ShipLength % 2 == 0)
        {
            shipCenter.x -= SizeCell / 2f;
        }

        // Tính toán vị trí của ô đầu tiên
        float startOffset = (ShipLength - 1) * SizeCell / 2f;
        Vector3 firstCellPos = new Vector3(shipCenter.x - startOffset, shipCenter.y, shipCenter.z);

        // Canvas để thêm UI elements
        Canvas canvas = transform.root.GetComponent<Canvas>();

        // Tạo khung cho toàn bộ vùng tàu sẽ đặt
        GameObject frame = new GameObject("ShipFrame");
        RectTransform frameTransform = frame.AddComponent<RectTransform>();

        // Gắn frame vào canvas
        frame.transform.SetParent(canvas.transform);
        frame.transform.SetAsLastSibling(); // Đưa lên trên cùng

        // Tính toán kích thước và vị trí của khung
        float width = ShipLength * SizeCell;
        float height = SizeCell;

        // Đặt vị trí và kích thước cho khung
        frameTransform.position = new Vector3(firstCellPos.x + width / 2 - SizeCell / 2, firstCellPos.y, firstCellPos.z);
        frameTransform.sizeDelta = new Vector2(width, height);
        frameTransform.localScale = Vector3.one;

        // Tạo đối tượng hình ảnh cho khung
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = isValid ? validFrameColor : invalidFrameColor;

        frameObjects.Add(frame);
    }

    private void ClearFrames()
    {
        foreach (GameObject frame in frameObjects)
        {
            if (frame != null)
            {
                Destroy(frame);
            }
        }
        frameObjects.Clear();
    }   

    // Đảm bảo xóa frames khi script bị hủy
    private void OnDestroy()
    {
        ClearFrames();
    }
}