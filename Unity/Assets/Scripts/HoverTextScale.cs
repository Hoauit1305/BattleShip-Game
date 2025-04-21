using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTextScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText;
    private Vector3 originalScale;
    public float scaleFactor = 1.1f;

    void Start()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        originalScale = buttonText.rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.rectTransform.localScale = originalScale * scaleFactor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.rectTransform.localScale = originalScale;
    }
}
