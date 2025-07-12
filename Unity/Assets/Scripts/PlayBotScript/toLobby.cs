using System.Collections; // 🔑 Cần thiết cho IEnumerator
using UnityEngine;
using UnityEngine.SceneManagement;

public class toLobby : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnClickToLobby()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            StartCoroutine(DelayLoadLobbyScene(0.1f)); // ⏱ Delay 0.1 giây sau khi phát âm
        }
        else
        {
            // Nếu không có AudioSource, load scene ngay
            SceneManager.LoadScene("LobbyScene");
        }
    }

    private IEnumerator DelayLoadLobbyScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("LobbyScene");
    }
}
