using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartButtonHandler : MonoBehaviour
{
    public GameObject loadingPanel;

    public void LoadNextScene()
    {
        StartCoroutine(LoadSceneWithDelay());
    }

    private IEnumerator LoadSceneWithDelay()
    {
        // Hiện panel
        loadingPanel.SetActive(true);

        // Chờ 0.5 giây
        yield return new WaitForSeconds(0.5f);

        // Chuyển scene
        SceneManager.LoadScene("AuthScene");
    }
}
