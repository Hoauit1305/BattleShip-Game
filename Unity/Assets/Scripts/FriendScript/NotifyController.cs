using UnityEngine;

public class NotifyController : MonoBehaviour
{
    public static NotifyController Instance;
    public GameObject NotifyFriendPanel;
    void Awake()
    {
        Instance = this;
    }

    public void ShowFriendNotify()
    {
        if (NotifyFriendPanel != null)
            NotifyFriendPanel.SetActive(true);
    }

    public void HideFriendNotify()
    {
        if (NotifyFriendPanel != null)
            NotifyFriendPanel.SetActive(false);
    }
}