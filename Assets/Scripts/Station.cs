using UnityEngine;
using System.Collections.Generic;
using System;

public class Station : MonoBehaviour
{
    private StationType station;
    public List<GameObject> commuters;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCommuter(GameObject commuter)
    {
        commuters.Add(commuter);
        commuter.transform.position = new Vector3 (Convert.ToSingle(transform.position.x+(.32*(commuters.Count-1))+.6), Convert.ToSingle(transform.position.y+.35), 0);
    }

    public StationType GetStationType()
    {
        return station;
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
