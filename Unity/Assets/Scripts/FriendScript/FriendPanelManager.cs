using UnityEngine;
using UnityEngine.UI;
public class OpenFriendPanel : MonoBehaviour
{
    public GameObject friendPanel;
    public GameObject listFriendPanel;
    public GameObject listRequestPanel;
    public GameObject searchFriendPanel;
    public ListFriend listFriendComponent;
    public ListRequest listRequestComponent;
    public GameObject chatPanel;
    public void ClickOpenFriendPanel()
    {
        friendPanel.SetActive(true);
    }
    public void ClickCloseFriendPanel()
    {
        friendPanel.SetActive(false);
    }
    public void ClickOpenListFriendPanel()
    {
        listFriendPanel.SetActive(true);
        listRequestPanel.SetActive(false);
        searchFriendPanel.SetActive(false);
    }
    public void ClickOpenListRequestPanel()
    {
        listFriendPanel.SetActive(false);
        listRequestPanel.SetActive(true);
        searchFriendPanel.SetActive(false);
    }
    public void ClickOpenSearchFriendPanel()
    {
        listFriendPanel.SetActive(false);
        listRequestPanel.SetActive(false);
        searchFriendPanel.SetActive(true);
    }
    public void OnRefreshButtonClick()
    {
        Debug.Log("👉 Nút Refresh được nhấn, làm mới danh sách bạn và yêu cầu kết bạn");

        if (listFriendComponent != null)
        {
            listFriendComponent.Refresh();
        }
        else
        {
            Debug.LogWarning("⚠️ listFriendComponent null");
        }

        if (listRequestComponent != null)
        {
            listRequestComponent.Refresh();
        }
        else
        {
            Debug.LogWarning("⚠️ listRequestComponent null");
        }
    }
    public void ClickCloseChatPanel()
    {
        chatPanel.SetActive(false);
    }
}
