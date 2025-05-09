using UnityEngine;

public class SlidePanelFromRight : MonoBehaviour
{
    public RectTransform panel;
    public float slideDuration = 0.4f;

    private Vector2 hiddenPos;
    private Vector2 visiblePos;
    private bool isVisible = false;

    void Start()
    {
        float panelWidth = panel.rect.width;

        // Vị trí hiển thị
        visiblePos = panel.anchoredPosition;

        // Vị trí ẩn: bên phải màn hình
        hiddenPos = visiblePos + new Vector2(panelWidth + 100f, 0); // +100 để chắc chắn nằm ngoài

        // Ẩn panel ban đầu
        panel.anchoredPosition = hiddenPos;
    }

    public void TogglePanel()
    {
        isVisible = !isVisible;

        Vector2 targetPos = isVisible ? visiblePos : hiddenPos;
        LeanTween.move(panel, targetPos, slideDuration).setEaseOutExpo();
    }
}
