using UnityEngine;
using UnityEngine.UI;

public class AvatarDisPlay : MonoBehaviour
{
    public Sprite[] avatarSprites; // Kéo đủ các avatar vào đây (theo đúng thứ tự ID)
    public Image displayImage;     // Image nơi bạn muốn hiển thị avatar

    void Start()
    {
        int id = PlayerPrefs.GetInt("SelectedAvatarID", 0); // Lấy ID đã lưu
        if (id >= 0 && id < avatarSprites.Length)
        {
            displayImage.sprite = avatarSprites[id];
        }
        else
        {
            Debug.LogWarning("Avatar ID không hợp lệ hoặc chưa được lưu.");
        }
    }
}
