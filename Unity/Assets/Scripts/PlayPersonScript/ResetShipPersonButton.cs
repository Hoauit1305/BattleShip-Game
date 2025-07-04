using UnityEngine;

public class ResetShipPersonButton : MonoBehaviour
{
    public ShipReset[] ships; // Kéo thả tất cả các tàu vào đây trong Unity Inspector

    public void OnResetClicked()
    {
        foreach (ShipReset ship in ships)
        {
            ship.ResetShip();
        }
    }
}
