using UnityEngine;
using UnityEditor;

public class AutoNameCells : EditorWindow
{
    Transform boardContainer;
    int rows = 10;
    int cols = 10;

    [MenuItem("Tools/Auto Name Cells")]
    public static void ShowWindow()
    {
        GetWindow<AutoNameCells>("Auto Name Cells");
    }

    void OnGUI()
    {
        GUILayout.Label("Board Naming Tool", EditorStyles.boldLabel);
        boardContainer = (Transform)EditorGUILayout.ObjectField("Board Container", boardContainer, typeof(Transform), true);
        rows = EditorGUILayout.IntField("Rows", rows);
        cols = EditorGUILayout.IntField("Columns", cols);

        if (GUILayout.Button("Auto Rename Cells"))
        {
            if (boardContainer == null)
            {
                Debug.LogError("Board container is not assigned!");
                return;
            }

            RenameCells();
        }
    }

    void RenameCells()
    {
        int totalCells = rows * cols;
        if (boardContainer.childCount < totalCells)
        {
            Debug.LogWarning("Not enough children in board container!");
            return;
        }

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            char rowChar = (char)('A' + row);
            for (int col = 1; col <= cols; col++)
            {
                Transform cell = boardContainer.GetChild(index);
                cell.name = $"{rowChar}{col}";
                index++;
            }
        }

        Debug.Log("✅ All cells renamed successfully!");
    }
}
