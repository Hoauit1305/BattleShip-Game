using UnityEngine;
using UnityEngine.SceneManagement;

public class toLobby : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnClickToLobby()
    {
        // Chuyển về LobbyScene
        SceneManager.LoadScene("LobbyScene");
    }
}
