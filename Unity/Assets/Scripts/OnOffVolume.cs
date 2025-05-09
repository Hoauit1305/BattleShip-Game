using UnityEngine;
using UnityEngine.UI;
public class OnOffVolume : MonoBehaviour
{
    public Sprite sprite1;
    public Sprite sprite2;

    private Image img;
    private bool showingFirst = true;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("Không tìm thấy Image component trên GameObject!");
        }
    }

    public void Toggle()
    {
        if (img == null || sprite1 == null || sprite2 == null) return;

        img.sprite = showingFirst ? sprite2 : sprite1;
        showingFirst = !showingFirst;
    }
}