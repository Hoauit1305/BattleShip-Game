using UnityEngine;

public class FireAudioManager : MonoBehaviour
{
    public static FireAudioManager Instance;
    public AudioClip fireClip;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi chuyển scene
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayFireSound()
    {
        if (fireClip != null)
            audioSource.PlayOneShot(fireClip);
    }
}
