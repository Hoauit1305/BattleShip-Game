using UnityEngine;
using UnityEngine.SceneManagement;

public class BotManager : MonoBehaviour
{
    
    public float waitTime = 3f; 

    void Start()
    {
        Invoke("LoadPlayScene", waitTime);
    }

    void LoadPlayScene()
    {
        SceneManager.LoadScene("PlayBotScene");
    }
}
