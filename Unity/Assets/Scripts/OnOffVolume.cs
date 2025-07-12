using UnityEngine;
using UnityEngine.UI;

public class OnOffVolume : MonoBehaviour
{
    public Sprite sprite1; // Icon khi có tiếng
    public Sprite sprite2; // Icon khi tắt tiếng

    private Image img;
    private bool showingFirst = true;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("Không tìm thấy Image component trên GameObject!");
        }
    }

    void Start()
    {
        // Lấy trạng thái từ PlayerPrefs, mặc định là bật (1)
        int isSoundOn = PlayerPrefs.GetInt("isSoundOn", 1);
        showingFirst = (isSoundOn == 1);

        // Đặt âm lượng và cập nhật icon đúng trạng thái
        AudioListener.volume = showingFirst ? 1f : 0f;
        if (img != null && sprite1 != null && sprite2 != null)
        {
            img.sprite = showingFirst ? sprite1 : sprite2;
        }
    }

    public void Toggle()
    {
        if (img == null || sprite1 == null || sprite2 == null) return;

        // Đổi trạng thái
        showingFirst = !showingFirst;

        // Đổi icon
        img.sprite = showingFirst ? sprite1 : sprite2;

        // Bật/tắt âm thanh
        AudioListener.volume = showingFirst ? 1f : 0f;

        // Lưu vào PlayerPrefs
        PlayerPrefs.SetInt("isSoundOn", showingFirst ? 1 : 0);
        PlayerPrefs.Save();
    }
}
