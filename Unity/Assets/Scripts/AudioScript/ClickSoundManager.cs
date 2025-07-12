using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class ClickSoundManager : MonoBehaviour
{
    public AudioClip clickSound;
    private AudioSource audioSource;
    private Button button;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0;

        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    public void PlayClick()
    {
        if (clickSound != null && audioSource.enabled && gameObject.activeInHierarchy)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}