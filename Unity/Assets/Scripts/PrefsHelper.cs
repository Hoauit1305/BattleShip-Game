using UnityEngine;

public static class PrefsHelper
{
    public static string GetKey(string key)
    {
#if UNITY_EDITOR
        if (ParrelSync.ClonesManager.IsClone())
        {
            return "clone_" + key;
        }
#endif
        return "main_" + key;
    }

    // Hàm hỗ trợ thao tác gọn hơn
    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(GetKey(key), value);
    }

    public static string GetString(string key)
    {
        return PlayerPrefs.GetString(GetKey(key), "");
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(GetKey(key), value);
    }

    public static int GetInt(string key)
    {
        return PlayerPrefs.GetInt(GetKey(key), 0);
    }

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll(); // hoặc chỉ xóa key theo GetKey() nếu muốn
    }
}
