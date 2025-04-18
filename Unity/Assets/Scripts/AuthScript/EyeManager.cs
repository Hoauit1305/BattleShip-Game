using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class EyeManager : MonoBehaviour
{
    public TMP_InputField pwInputField;

    public GameObject EyeCloseButton;
    public GameObject EyeOpenButton;

    public void OnEyeCloseButtonClicked()
    {
        EyeCloseButton.SetActive(false);
        EyeOpenButton.SetActive(true);
        pwInputField.contentType = TMP_InputField.ContentType.Standard;
        pwInputField.ForceLabelUpdate();
    }
    public void OnEyeOpenButtonClicked()
    {
        EyeCloseButton.SetActive(true);
        EyeOpenButton.SetActive(false);
        pwInputField.contentType = TMP_InputField.ContentType.Password;
        pwInputField.ForceLabelUpdate();
    }
}