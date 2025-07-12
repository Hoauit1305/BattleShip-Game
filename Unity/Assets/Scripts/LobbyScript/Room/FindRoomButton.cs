using UnityEngine;
using TMPro;
using System.Collections;

public class FindRoomButton : MonoBehaviour
{
    public TMP_InputField RoomCodeInputField;
    public SwitchPanelButton switchPanelButton; // Thêm tham chiếu đến SwitchPanelButton
    private AudioSource audioSource; // ✅ Thêm biến AudioSource

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>(); // ✅ Gán AudioSource
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện tìm phòng thành công từ RoomManager
        RoomManager.Instance.OnRoomJoinSuccess += HandleRoomJoinSuccess;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện khi đối tượng bị hủy để tránh memory leak
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomJoinSuccess -= HandleRoomJoinSuccess;
        }
    }

    public void OnFindRoomClickedButton()
    {
        // ✅ Phát âm thanh click trước khi tìm phòng
        if (audioSource != null && audioSource.clip != null && audioSource.enabled)
        {
            audioSource.Play();
        }

        string RoomCode = RoomCodeInputField.text;
        RoomManager.Instance.FindRoom(RoomCode);
    }

    // Hàm xử lý khi tìm phòng thành công
    private void HandleRoomJoinSuccess()
    {
        // Gọi SwitchPanelButton để chuyển scene
        if (switchPanelButton != null)
        {
            switchPanelButton.SwitchPanel();
        }
        else
        {
            Debug.LogWarning("SwitchPanelButton chưa được gán!");
        }
    }
}
