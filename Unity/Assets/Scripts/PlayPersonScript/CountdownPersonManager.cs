using System.Collections;
using UnityEngine;
using UnityEngine.UI; // dùng Text thường
 using TMPro;

public class CountdownPersonManager : MonoBehaviour
{
    public TMP_Text countdownText; // hoặc TMP_Text nếu dùng TMP
    //public GameObject countdownUI;
    public GameObject CountdownPanel;
    public IEnumerator StartCountdown(System.Action onComplete)
    {
        //countdownUI.SetActive(true);
        CountdownPanel.SetActive(true);
        string[] steps = { "3", "2", "1", "READY", "GO!" };

        foreach (string step in steps)
        {
            countdownText.text = step;
            countdownText.transform.localScale = Vector3.zero;
            LeanTween.scale(countdownText.gameObject, Vector3.one, 0.3f).setEaseOutBack();
            yield return new WaitForSeconds(1f);
        }

        countdownText.gameObject.SetActive(false);
        CountdownPanel.SetActive(false);
        onComplete?.Invoke(); // gọi hành động tiếp theo (bắt đầu game)
    }
}
