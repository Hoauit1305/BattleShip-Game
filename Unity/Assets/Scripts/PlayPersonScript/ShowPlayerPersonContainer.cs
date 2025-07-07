using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ShowPlayerPersonContainer : MonoBehaviour
{
    // Thêm prefab để hiển thị tàu của player
    public GameObject ship2Prefab;
    public GameObject ship31Prefab;
    public GameObject ship32Prefab;
    public GameObject ship4Prefab;
    public GameObject ship5Prefab;

    // Thêm tham chiếu đến bảng hiển thị tàu player (bảng bên phải)
    [Header("Player Ship Board")]
    public Transform playerShipBoard; // Kéo thả bảng bên phải vào đây

    // Dictionary để theo dõi các tàu đã được đặt trên bảng
    private Dictionary<string, GameObject> placedPlayerShips = new Dictionary<string, GameObject>();

    void Start()
    {
        // Kiểm tra xem đã assign playerShipBoard chưa
        if (playerShipBoard == null)
        {
            Debug.LogError("PlayerShipBoard chưa được assign! Vui lòng kéo thả bảng hiển thị tàu vào field PlayerShipBoard.");
            return;
        }

        // Tắt hình ảnh của tất cả các prefab tàu khi khởi tạo
        DisableAllShipPrefabImages();

        // Gọi API để lấy và hiển thị tàu của player
        StartCoroutine(LoadAndShowPlayerShips());
    }

    // Hàm tắt hình ảnh của tất cả các prefab tàu
    void DisableAllShipPrefabImages()
    {
        if (ship2Prefab != null) ship2Prefab.GetComponent<Image>().enabled = false;
        if (ship31Prefab != null) ship31Prefab.GetComponent<Image>().enabled = false;
        if (ship32Prefab != null) ship32Prefab.GetComponent<Image>().enabled = false;
        if (ship4Prefab != null) ship4Prefab.GetComponent<Image>().enabled = false;
        if (ship5Prefab != null) ship5Prefab.GetComponent<Image>().enabled = false;
    }

    // Coroutine để gọi API và hiển thị tàu
    IEnumerator LoadAndShowPlayerShips()
    {
        string gameId = PrefsHelper.GetInt("gameId").ToString();
        string apiURL = "https://battleship-game-production.up.railway.app/api/gameplay/showship";

        // Tạo request body
        ShowShipPersonRequest request = new ShowShipPersonRequest(gameId);
        UnityWebRequest webRequest = CreatePostRequest(apiURL, request);

        Debug.Log($"Gửi request để lấy thông tin tàu player với gameId: {gameId}");
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            string responseText = webRequest.downloadHandler.text;
            Debug.Log($"API response: {responseText}");

            try
            {
                // Parse JSON response
                PlayerPersonShipData[] playerShips = JsonPersonHelper.FromJson<PlayerPersonShipData>(responseText);

                if (playerShips != null && playerShips.Length > 0)
                {
                    Debug.Log($"Nhận được {playerShips.Length} tàu từ API");

                    // Hiển thị từng tàu
                    foreach (PlayerPersonShipData ship in playerShips)
                    {
                        ShowPlayerShip(ship);
                    }
                }
                else
                {
                    Debug.LogWarning("Không có dữ liệu tàu nào được trả về từ API");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Lỗi khi parse JSON: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"API error: {webRequest.error}");
        }
    }

    // Hàm tìm ô trong bảng player ship (bảng bên phải)
    GameObject FindCellInPlayerBoard(string cellName)
    {
        if (playerShipBoard == null)
        {
            Debug.LogError("PlayerShipBoard không được assign!");
            return null;
        }

        // Tìm ô con trong playerShipBoard
        Transform cellTransform = playerShipBoard.Find(cellName);
        if (cellTransform != null)
        {
            return cellTransform.gameObject;
        }

        // Nếu không tìm thấy bằng Find, thử tìm trong tất cả con của playerShipBoard
        foreach (Transform child in playerShipBoard)
        {
            if (child.name == cellName)
            {
                return child.gameObject;
            }
        }

        Debug.LogError($"Không tìm thấy ô {cellName} trong bảng player ship!");
        return null;
    }

    // Hàm hiển thị tàu của player
    void ShowPlayerShip(PlayerPersonShipData shipData)
    {
        Debug.Log($"Hiển thị tàu player: {shipData.shipType} tại {string.Join(", ", shipData.positions)}");

        // Xác định prefab tàu dựa trên shipType
        GameObject shipPrefab = GetShipPrefabByType(shipData.shipType);

        if (shipPrefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho tàu: {shipData.shipType}");
            return;
        }

        // Tạo key duy nhất cho tàu này
        string shipKey = $"{shipData.shipType}_{string.Join("_", shipData.positions)}";
        GameObject shipInstance;

        if (placedPlayerShips.ContainsKey(shipKey))
        {
            // Nếu tàu đã tồn tại, sử dụng lại
            shipInstance = placedPlayerShips[shipKey];
            Debug.Log($"Tàu {shipKey} đã tồn tại, cập nhật vị trí");
        }
        else
        {
            // Lấy ô đầu tiên để đặt tàu TRONG BẢNG PLAYER SHIP
            GameObject firstCell = FindCellInPlayerBoard(shipData.positions[0]);
            if (firstCell == null)
            {
                Debug.LogError($"Không tìm thấy ô: {shipData.positions[0]} trong bảng player ship");
                return;
            }

            // Tạo instance mới của tàu
            shipInstance = Instantiate(shipPrefab, firstCell.transform);
            shipInstance.name = $"Player_{shipData.shipType}";

            // Lưu vào dictionary
            placedPlayerShips.Add(shipKey, shipInstance);
            Debug.Log($"Tàu mới {shipKey} đã được tạo trong bảng player ship");
        }

        // Đặt tàu vào vị trí đầu tiên TRONG BẢNG PLAYER SHIP
        GameObject startCell = FindCellInPlayerBoard(shipData.positions[0]);
        if (startCell != null)
        {
            // Di chuyển tàu đến ô đầu tiên
            shipInstance.transform.SetParent(startCell.transform);
            shipInstance.transform.localPosition = Vector3.zero;

            // Xác định hướng tàu
            bool isVertical = IsShipVertical(shipData.positions);

            // Cấu hình hình ảnh và kích thước của tàu
            ConfigurePlayerShipVisual(shipInstance, shipData, isVertical);

            // Bật (enable) hình ảnh của tàu
            Image shipImage = shipInstance.GetComponent<Image>();
            if (shipImage != null)
            {
                shipImage.enabled = true;
                Debug.Log($"Đã bật hình ảnh cho tàu player {shipData.shipType}");
            }
            else
            {
                Debug.LogError($"Không tìm thấy component Image trên tàu {shipData.shipType}");
            }
        }
        else
        {
            Debug.LogError($"Không tìm thấy ô bắt đầu: {shipData.positions[0]} trong bảng player ship");
        }
    }

    // Xác định tàu đặt theo chiều dọc hay ngang
    bool IsShipVertical(string[] positions)
    {
        if (positions.Length <= 1) return false;

        // Lấy ký tự đầu tiên (chữ cái) của vị trí đầu tiên và thứ hai
        char firstLetter = positions[0][0];
        char secondLetter = positions[1][0];

        // Nếu chữ cái khác nhau => tàu đặt theo chiều dọc
        return firstLetter != secondLetter;
    }

    // Lấy prefab tàu dựa vào loại
    GameObject GetShipPrefabByType(string shipType)
    {
        switch (shipType)
        {
            case "Ship2": return ship2Prefab;
            case "Ship3.1": return ship31Prefab;
            case "Ship3.2": return ship32Prefab;
            case "Ship4": return ship4Prefab;
            case "Ship5": return ship5Prefab;
            default: return null;
        }
    }

    // Thiết lập hiển thị cho tàu player (kích thước, hướng)
    void ConfigurePlayerShipVisual(GameObject shipObject, PlayerPersonShipData shipData, bool isVertical)
    {
        RectTransform rectTransform = shipObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Lấy kích thước của một ô TRONG BẢNG PLAYER SHIP
        GameObject firstCell = FindCellInPlayerBoard(shipData.positions[0]);
        RectTransform cellRect = firstCell?.GetComponent<RectTransform>();
        if (cellRect == null) return;
        float cellSize = cellRect.rect.width;

        // Số ô mà tàu chiếm
        int shipSize = GetShipSizeFromType(shipData.shipType);

        // Reset rotation và scale trước khi điều chỉnh
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        // Giảm kích thước để tránh dính lưới
        float scaleFactor = 0.9f;

        // Thiết lập kích thước tàu ngang (cơ bản)
        rectTransform.sizeDelta = new Vector2(cellSize * shipSize * scaleFactor, cellSize * scaleFactor);

        // Nếu là tàu dọc thì xoay 90 độ
        if (isVertical)
        {
            rectTransform.Rotate(0, 0, 90);
        }

        // Lấy các ô đầu và cuối để xác định vị trí chính xác TRONG BẢNG PLAYER SHIP
        string firstCellName = shipData.positions[0];
        string lastCellName = shipData.positions[shipData.positions.Length - 1];

        GameObject firstCellObj = FindCellInPlayerBoard(firstCellName);
        GameObject lastCellObj = FindCellInPlayerBoard(lastCellName);

        if (firstCellObj != null && lastCellObj != null)
        {
            Vector3 firstPos = firstCellObj.transform.position;
            Vector3 lastPos = lastCellObj.transform.position;

            // Xác định vị trí trung tâm của tàu
            Vector3 centerPos = (firstPos + lastPos) / 2f;

            // Di chuyển tàu đến vị trí trung tâm
            rectTransform.position = centerPos;
        }

        // Đảm bảo tàu hiển thị phía trên các phần tử khác
        shipObject.transform.SetAsLastSibling();

        // Đảm bảo hình ảnh của tàu được hiển thị rõ ràng
        Image shipImage = shipObject.GetComponent<Image>();
        if (shipImage != null)
        {
            shipImage.raycastTarget = false;

            if (shipImage is UnityEngine.UI.Image)
            {
                UnityEngine.UI.Image uiImage = shipImage as UnityEngine.UI.Image;
                if (uiImage.type == UnityEngine.UI.Image.Type.Sliced)
                {
                    uiImage.pixelsPerUnitMultiplier = 1;
                }
            }
        }
    }

    // Lấy kích thước tàu từ loại tàu
    int GetShipSizeFromType(string shipType)
    {
        switch (shipType)
        {
            case "Ship2": return 2;
            case "Ship3.1":
            case "Ship3.2": return 3;
            case "Ship4": return 4;
            case "Ship5": return 5;
            default: return 1;
        }
    }

    // Tạo POST request
    UnityWebRequest CreatePostRequest(string url, ShowShipPersonRequest requestData)
    {
        string jsonBody = JsonUtility.ToJson(requestData);
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        string token = PrefsHelper.GetString("token");
        request.SetRequestHeader("Authorization", "Bearer " + token);

        return request;
    }

    // Hàm public để refresh tàu (có thể gọi từ bên ngoài)
    public void RefreshPlayerShips()
    {
        // Xóa tất cả tàu hiện tại
        foreach (var ship in placedPlayerShips.Values)
        {
            if (ship != null)
            {
                Destroy(ship);
            }
        }
        placedPlayerShips.Clear();

        // Load lại tàu từ API
        StartCoroutine(LoadAndShowPlayerShips());
    }
}

// Cấu trúc dữ liệu cho request
[System.Serializable]
public class ShowShipPersonRequest
{
    public string gameId;

    public ShowShipPersonRequest(string gameId)
    {
        this.gameId = gameId;
    }
}

// Helper class để parse JSON array
public static class JsonPersonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(CreateWrapperJson(json));
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    private static string CreateWrapperJson(string json)
    {
        return "{ \"Items\": " + json + "}";
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

// Cấu trúc dữ liệu cho response
[System.Serializable]
public class PlayerPersonShipData
{
    public string shipType;
    public string[] positions;
}