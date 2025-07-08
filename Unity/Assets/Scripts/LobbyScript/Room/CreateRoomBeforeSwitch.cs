using UnityEngine;

public class CreateRoomBeforeSwitch : MonoBehaviour
{
    private SwitchPanelButton switchPanelButton;

    private void Awake()
    {
        switchPanelButton = GetComponent<SwitchPanelButton>();
    }

    public void CreateRoomAndSwitch()
    {
        // Gọi Create Room trước
        RoomManager.Instance.CreateRoom();

        // Sau đó Switch Panel
        if (switchPanelButton != null)
        {
            switchPanelButton.SwitchPanel();
        }
    }
}
