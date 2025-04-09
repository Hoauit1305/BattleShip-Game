using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    public void LoadNextScene()
    {
        SceneManager.LoadScene("AuthScene");
    }
}
