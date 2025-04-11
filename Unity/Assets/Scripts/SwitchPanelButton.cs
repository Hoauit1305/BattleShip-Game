using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchPanelButton : MonoBehaviour
{
    public GameObject SourcePanel;
    public GameObject DestinationPanel;

    public void SwitchPanel()
    {
        if (SourcePanel != null) SourcePanel.SetActive(false);
        if (DestinationPanel != null) DestinationPanel.SetActive(true);
    }
}
