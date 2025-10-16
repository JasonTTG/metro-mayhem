using UnityEngine;

public class Station : MonoBehaviour
{
    private enum StationType
    {
        Circle,
        Square,
        Triangle
    }
    private StationType station;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStation(int type)
    {
        switch (type)
        {
            case 0:
                station = StationType.Circle;
                transform.Find("Circle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case 1:
                station = StationType.Square;
                transform.Find("Square_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
            case 2:
                station = StationType.Triangle;
                transform.Find("Triangle_0").GetComponent<SpriteRenderer>().enabled = true;
                break;
        }
    }
}
