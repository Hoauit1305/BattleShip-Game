using UnityEngine;

public class ShipPersonReset : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        // Lưu lại vị trí và hướng ban đầu khi game bắt đầu
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void ResetShip()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
