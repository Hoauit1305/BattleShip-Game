using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class SelectAvatar : MonoBehaviour
{
    public RectTransform frame;    
    public RectTransform[] avatarButtons;

    // Hàm này sẽ được gọi khi người dùng chọn avatar
    public void MoveKhung(int index)
    {
        if (index >= 0 && index < avatarButtons.Length)
        {
            frame.position = avatarButtons[index].position;
        }
    }
}
