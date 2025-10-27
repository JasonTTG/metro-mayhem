using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class Commuter : MonoBehaviour
{
    
    public StationType type;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCommuter(StationType destination) {
        type = destination;
        switch (destination)
        {
            case StationType.Circle:
                transform.Find("Circle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case StationType.Square:
                transform.Find("Square_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case StationType.Triangle:
                transform.Find("Triangle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
        }
    }
}
