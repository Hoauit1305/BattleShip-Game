using System.Collections;
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
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>(); // ✅ Lấy AudioSource
    }

    public void ClickOpenFriendPanel()
    {
        friendPanel.SetActive(true);
    }
    public void ClickCloseFriendPanel()
    {
        // ✅ Nếu có tiếng → phát trước rồi mới tắt panel sau 0.1s
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            StartCoroutine(DelayHideFriendPanel(0.1f)); // ✅ Delay
        }
        else
        {
            friendPanel.SetActive(false); // Nếu không có tiếng → tắt ngay
        }
    }

    private IEnumerator DelayHideFriendPanel(float delay)
    {
        yield return new WaitForSeconds(delay);
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
            Debug.LogWarning("⚠ listFriendComponent null");
        }

        if (listRequestComponent != null)
        {
            listRequestComponent.Refresh();
        }
        else
        {
            Debug.LogWarning("⚠ listRequestComponent null");
        }
    }
    public void ClickCloseChatPanel()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            StartCoroutine(DelayCloseChatPanel(0.1f)); // ⏱ Delay 0.1 giây
        }
        else
        {
            chatPanel.SetActive(false); // Nếu không có tiếng thì tắt ngay
        }
    }

    private IEnumerator DelayCloseChatPanel(float delay)
    {
        yield return new WaitForSeconds(delay);
        chatPanel.SetActive(false);
    }
}
