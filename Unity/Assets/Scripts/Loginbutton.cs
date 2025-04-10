using UnityEngine;
using UnityEngine.SceneManagement;

public class Loginbutton : MonoBehaviour
{
    public void LoadNextScene()
    {
        SceneManager.LoadScene("LoginScene");
    }
}
