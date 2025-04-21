using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using System.Linq;

public class PlaceShip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    public int ShipLength;
    public float SizeCell;
    private bool isVertical = false;
    public RectTransform gridTransform;
    private List<GameObject> frameObjects = new List<GameObject>();
    public Color validFrameColor = Color.green;
    public Color invalidFrameColor = Color.red;
    public static List<RectTransform> placedShips = new List<RectTransform>();
    private Vector2 originalSize;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalSize = rectTransform.sizeDelta;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RotateShip();
    }

    private void RotateShip()
    {
        Vector3 originalPosition = rectTransform.position;
        isVertical = !isVertical;
        rectTransform.Rotate(0, 0, 90);

        if (ShipLength % 2 == 0)
        {
            Vector2[] adjustments = new Vector2[]
            {
                new Vector2(SizeCell / 2f, SizeCell / 2f),
                new Vector2(SizeCell / 2f, -SizeCell / 2f),
                new Vector2(-SizeCell / 2f, SizeCell / 2f),
                new Vector2(-SizeCell / 2f, -SizeCell / 2f)
            };

            bool foundValidPosition = false;

            foreach (var adjustment in adjustments)
            {
                Vector3 newPosition = originalPosition;
                newPosition.x += adjustment.x;
                newPosition.y += adjustment.y;

                if (IsShipPlacementValid(newPosition))
                {
                    rectTransform.position = newPosition;
                    foundValidPosition = true;
                    break;
                }
            }

            if (!foundValidPosition)
            {
                isVertical = !isVertical;
                rectTransform.Rotate(0, 0, -90);
                rectTransform.position = originalPosition;
            }
        }
        else
        {
            if (!IsShipPlacementValid(rectTransform.position))
            {
                isVertical = !isVertical;
                rectTransform.Rotate(0, 0, -90);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.position;
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / transform.root.GetComponent<Canvas>().scaleFactor;

        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        ClearFrames();

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag("GridCell"))
            {
                Vector3 cellPosition = result.gameObject.transform.position;
                Vector3 shipPosition = cellPosition;
                if (ShipLength % 2 == 0)
                {
                    if (!isVertical)
                        shipPosition.x -= SizeCell / 2f;
                    else
                        shipPosition.y -= SizeCell / 2f;
                }
                bool isValid = IsShipPlacementValid(shipPosition);
                ShowShipFrame(cellPosition, isValid);
                break;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        ClearFrames();

        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        bool placed = false;

        foreach (var result in raycastResults)
        {
            if (result.gameObject.CompareTag("GridCell"))
            {
                Vector3 cellPosition = result.gameObject.transform.position;
                Vector3 newShipPosition = cellPosition;

                if (ShipLength % 2 == 0)
                {
                    if (!isVertical)
                        newShipPosition.x -= SizeCell / 2f;
                    else
                        newShipPosition.y -= SizeCell / 2f;
                }

                if (IsShipPlacementValid(newShipPosition))
                {
                    rectTransform.position = newShipPosition;
                    if (!placedShips.Contains(rectTransform))
                        placedShips.Add(rectTransform);
                    placed = true;
                }
            }
        }

        if (!placed)
            rectTransform.position = originalPosition;
    }

    private bool IsShipPlacementValid(Vector3 shipPosition)
    {
        if (gridTransform == null)
        {
            Debug.LogError("Grid Transform is not assigned!");
            return false;
        }

        Vector3[] gridCorners = new Vector3[4];
        gridTransform.GetWorldCorners(gridCorners);
        float gridMinX = gridCorners[0].x;
        float gridMaxX = gridCorners[2].x;
        float gridMinY = gridCorners[0].y;
        float gridMaxY = gridCorners[2].y;

        float shipWidth = isVertical ? SizeCell : ShipLength * SizeCell;
        float shipHeight = isVertical ? ShipLength * SizeCell : SizeCell;

        float shipMinX = shipPosition.x - shipWidth / 2f;
        float shipMaxX = shipPosition.x + shipWidth / 2f;
        float shipMinY = shipPosition.y - shipHeight / 2f;
        float shipMaxY = shipPosition.y + shipHeight / 2f;

        Rect shipRect = new Rect(shipMinX, shipMinY, shipWidth, shipHeight);

        if (shipMinX < gridMinX || shipMaxX > gridMaxX || shipMinY < gridMinY || shipMaxY > gridMaxY)
            return false;

        foreach (var placed in placedShips)
        {
            if (placed == rectTransform) continue;

            Vector3 placedPos = placed.position;
            PlaceShip placedShip = placed.GetComponent<PlaceShip>();
            Vector2 placedSize = placedShip != null && placedShip.isVertical
                ? new Vector2(placedShip.SizeCell, placedShip.ShipLength * placedShip.SizeCell)
                : new Vector2(placedShip.ShipLength * placedShip.SizeCell, placedShip.SizeCell);

            Rect placedRect = new Rect(
                placedPos.x - placedSize.x / 2f,
                placedPos.y - placedSize.y / 2f,
                placedSize.x,
                placedSize.y
            );

            if (shipRect.Overlaps(placedRect))
                return false;
        }

        return true;
    }

    private void ShowShipFrame(Vector3 startCellPosition, bool isValid)
    {
        Vector3 shipCenter = startCellPosition;
        if (ShipLength % 2 == 0)
        {
            if (!isVertical)
                shipCenter.x -= SizeCell / 2f;
            else
                shipCenter.y -= SizeCell / 2f;
        }

        Canvas canvas = transform.root.GetComponent<Canvas>();
        GameObject frame = new GameObject("ShipFrame");
        RectTransform frameTransform = frame.AddComponent<RectTransform>();
        frame.transform.SetParent(canvas.transform);
        frame.transform.SetAsLastSibling();

        float width = isVertical ? SizeCell : ShipLength * SizeCell;
        float height = isVertical ? ShipLength * SizeCell : SizeCell;
        Vector3 framePosition = shipCenter;
        frameTransform.position = framePosition;
        frameTransform.sizeDelta = new Vector2(width, height);
        frameTransform.localScale = Vector3.one;

        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = isValid ? validFrameColor : invalidFrameColor;
        frameObjects.Add(frame);
    }

    private void ClearFrames()
    {
        foreach (GameObject frame in frameObjects)
        {
            if (frame != null)
                Destroy(frame);
        }
        frameObjects.Clear();
    }

    private void OnDestroy()
    {
        ClearFrames();
    }

    public static string ExportShipsAsGameJSON(int gameId = 1)
    {
        var json = new StringBuilder();
        json.Append("{\n");
        json.AppendFormat("  \"gameId\": {0},\n", gameId);
        json.Append("  \"ships\": [\n");

        for (int i = 0; i < placedShips.Count; i++)
        {
            RectTransform ship = placedShips[i];
            PlaceShip placeShip = ship.GetComponent<PlaceShip>();
            if (placeShip == null) continue;

            List<string> occupiedCells = GetOccupiedCellNames(placeShip);
            string type = placeShip.gameObject.name;

            json.Append("    {\n");
            json.AppendFormat("      \"type\": \"{0}\",\n", type);
            json.Append("      \"positions\": [");

            for (int j = 0; j < occupiedCells.Count; j++)
            {
                json.AppendFormat("\"{0}\"", occupiedCells[j]);
                if (j < occupiedCells.Count - 1)
                    json.Append(", ");
            }

            json.Append("]\n    }");
            if (i < placedShips.Count - 1)
                json.Append(",\n");
            else
                json.Append("\n");
        }

        json.Append("  ]\n}");
        return json.ToString();
    }

    private static List<string> GetOccupiedCellNames(PlaceShip ship)
    {
        List<string> occupied = new List<string>();
        float halfCell = ship.SizeCell / 2f;
        Vector3 shipCenter = ship.rectTransform.position;
        int length = ship.ShipLength;

        for (int i = 0; i < length; i++)
        {
            Vector3 offset = Vector3.zero;
            if (ship.isVertical)
                offset.y = (i - (length - 1) / 2f) * ship.SizeCell;
            else
                offset.x = (i - (length - 1) / 2f) * ship.SizeCell;

            Vector3 cellPos = shipCenter + offset;
            GameObject closestCell = FindClosestGridCell(cellPos);
            if (closestCell != null)
                occupied.Add(closestCell.name);
        }

        return occupied;
    }

    private static GameObject FindClosestGridCell(Vector3 worldPos)
    {
        GameObject[] allCells = GameObject.FindGameObjectsWithTag("GridCell");
        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (GameObject cell in allCells)
        {
            float dist = Vector3.Distance(cell.transform.position, worldPos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = cell;
            }
        }

        return closest;
    }
}
