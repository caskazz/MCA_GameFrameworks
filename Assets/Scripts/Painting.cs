using UnityEngine;

public class Crop : MonoBehaviour
{
    
    [SerializeField] public int scoreValue = 100;
    private Renderer cropRenderer;


    void Start()
    {

        cropRenderer = GetComponentInChildren<Renderer>();

    }

    public int GetScoreValue()
    {
        return scoreValue;
    }

}
