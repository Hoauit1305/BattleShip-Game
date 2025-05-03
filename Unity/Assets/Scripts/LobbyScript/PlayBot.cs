using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayBot : MonoBehaviour
{
    public void OnClickFightBot()
    {
        // Chuyển sang scene FindMatchesScene
        SceneManager.LoadScene("FindMatchesScene");
    }
}
