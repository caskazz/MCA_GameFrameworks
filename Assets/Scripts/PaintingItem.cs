using UnityEngine;

// Put this on every painting that can be picked up and sold (alongside the "PickUp" tag).
public class PaintingItem : MonoBehaviour
{
    [SerializeField] private string paintingName = "Sunset";
    [SerializeField] private int price = 100;

    public string PaintingName => paintingName;
    public int Price => price;
}