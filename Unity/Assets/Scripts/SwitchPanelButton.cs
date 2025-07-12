using UnityEngine;

public class SwitchPanelButton : MonoBehaviour
{
    public GameObject SourcePanel;
    public GameObject DestinationPanel;
    public float delayBeforeSwitch = 0.1f;
    public ClickSoundManager clickSoundManager;

    public void SwitchPanel()
    {
        // 🔊 Phát tiếng click
        if (clickSoundManager != null)
            clickSoundManager.PlayClick();

        // ⏳ Delay sau đó mới chuyển panel
        Invoke(nameof(DoSwitchPanel), delayBeforeSwitch);
    }

    void DoSwitchPanel()
    {
        if (SourcePanel != null) SourcePanel.SetActive(false);
        if (DestinationPanel != null) DestinationPanel.SetActive(true);
    }
}
