using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class HomeButton : MonoBehaviour
{
    public GameObject loadingPanel;
    public void OnHomeButtonClicked()
    {
        StartCoroutine(HomeCoroutine());
    }
    IEnumerator HomeCoroutine()
    {
        loadingPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("LobbyScene");
    }
}
