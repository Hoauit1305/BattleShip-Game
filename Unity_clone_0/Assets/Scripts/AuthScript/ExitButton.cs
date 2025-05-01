using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public void OnClickedExitGame()
    {
        Debug.Log("Thoát game!");

        // Khi build ra game thật, lệnh này mới có tác dụng
        Application.Quit();
    }
}
