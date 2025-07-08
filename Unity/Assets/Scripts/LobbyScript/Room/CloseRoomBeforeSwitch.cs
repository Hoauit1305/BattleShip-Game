using UnityEngine;
public class CloseRoomBeforeSwitch : MonoBehaviour
{
    private SwitchPanelButton switchPanelButton;

    private void Awake()
    {
        switchPanelButton = GetComponent<SwitchPanelButton>();
    }

    public void CloseRoomAndSwitch()
    {
        // Kiểm tra xem người chơi hiện tại có phải là chủ phòng không
        if (RoomManager.Instance.IsRoomOwner())
        {
            // Nếu là chủ phòng thì mới đóng phòng
            RoomManager.Instance.CloseRoom();
        }
        else
        {
            // Nếu không phải chủ phòng thì chỉ rời phòng (không đóng)
            RoomManager.Instance.LeaveRoom();
        }

        // Sau đó Switch Panel
        if (switchPanelButton != null)
        {
            switchPanelButton.SwitchPanel();
        }
    }
}