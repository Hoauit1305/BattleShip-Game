using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public GameObject chooseNamePanel; 
    public GameObject lobbyPanel;
    void Start()
    {
        int hasName = PlayerPrefs.GetInt("hasName", 0);

        if (hasName == 0)
        {
            chooseNamePanel.SetActive(true);
            lobbyPanel.SetActive(false);
        }
        else
        {
            chooseNamePanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }
    }
}
