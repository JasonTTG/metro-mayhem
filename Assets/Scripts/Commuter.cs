using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class Commuter : MonoBehaviour
{
    public enum CommuterType
    {
        Circle,
        Square,
        Triangle
    }
    public CommuterType type;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCommuter(int destination) {
        switch (destination)
        {
            case 0:
                type = CommuterType.Circle;
                transform.Find("Circle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case 1:
                type = CommuterType.Square;
                transform.Find("Square_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case 2:
                type = CommuterType.Triangle;
                transform.Find("Triangle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
        }
    }
}
