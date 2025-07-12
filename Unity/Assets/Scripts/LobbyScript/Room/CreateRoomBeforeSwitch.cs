using UnityEngine;

public class CreateRoomBeforeSwitch : MonoBehaviour
{
    private SwitchPanelButton switchPanelButton;
    private AudioSource audioSource;

    private void Awake()
    {
        switchPanelButton = GetComponent<SwitchPanelButton>();
        audioSource = GetComponent<AudioSource>(); // ✅ Thêm dòng này để lấy AudioSource
    }

    public void CreateRoomAndSwitch()
    {
        // ✅ Phát âm thanh nếu có AudioSource và Clip
        if (audioSource != null && audioSource.clip != null && audioSource.enabled)
        {
            audioSource.Play(); // Có thể thay bằng PlayOneShot nếu cần
        }

        // Gọi Create Room trước
        RoomManager.Instance.CreateRoom();

        // Sau đó Switch Panel
        if (switchPanelButton != null)
        {
            switchPanelButton.SwitchPanel();
        }
    }
}