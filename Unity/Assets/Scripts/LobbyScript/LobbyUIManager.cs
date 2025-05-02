using UnityEngine;

public class LobbyUIManager : MonoBehaviour
{
    public GameObject chooseNamePanel; 
    public GameObject lobbyPanel;
    void Start()
    {
        int hasName = PrefsHelper.GetInt("hasName");
        Debug.Log("hasname: " + hasName);
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
