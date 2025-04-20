using UnityEngine;
using UnityEngine.UI;

public class AvatarSelector : MonoBehaviour
{
    public int avatarID; // Gán giá trị này từ Inspector
    public Image avatarImage; // Ảnh đại diện của button (dùng để hiển thị ở nơi khác)

    public void OnAvatarSelected()
    {
        PlayerPrefs.SetInt("SelectedAvatarID", avatarID);
        PlayerPrefs.Save();
        Debug.Log("Đã chọn avatar ID: " + avatarID);
    }
}
