using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Button))]
public class SwitchPanelButton : MonoBehaviour
{
    public GameObject SourcePanel;
    public GameObject DestinationPanel;
    public AudioClip clickSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Đăng ký sự kiện click
        GetComponent<Button>().onClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        Debug.Log($"{gameObject.name} Clicked — SFX: {clickSound?.name}");

        if (clickSound != null)
            audioSource.PlayOneShot(clickSound);

        Invoke(nameof(SwitchPanel), 0.05f);
    }

    public void SwitchPanel()
    {
        if (SourcePanel != null) SourcePanel.SetActive(false);
        if (DestinationPanel != null) DestinationPanel.SetActive(true);
    }
}
