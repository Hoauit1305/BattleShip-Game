using UnityEngine;
using UnityEngine.SceneManagement;

public class ShowLoginButton : MonoBehaviour
{
    public GameObject LoginPanel;
    public GameObject MainPanel;

    public void ShowLogin()
    {
        LoginPanel.SetActive(true);
        MainPanel.SetActive(false);
    }
}
