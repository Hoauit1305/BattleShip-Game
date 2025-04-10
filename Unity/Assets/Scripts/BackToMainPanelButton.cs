using UnityEngine;

public class BackToMainPanelButton : MonoBehaviour
{
    public GameObject MainPanel;
    public GameObject[] otherPanels;

    public void Back()
    {
        // Ẩn tất cả các panel phụ
        foreach (GameObject panel in otherPanels)
        {
            panel.SetActive(false);
        }

        // Hiện lại MainPanel
        MainPanel.SetActive(true);
    }
}
