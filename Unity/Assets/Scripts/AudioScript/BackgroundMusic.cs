using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    void Awake()
    {
        // Kiểm tra nếu đã tồn tại đối tượng BackgroundMusic khác
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Không hủy khi chuyển scene
        }
        else
        {
            Destroy(gameObject); // Hủy các đối tượng trùng lặp
        }
    }
}
