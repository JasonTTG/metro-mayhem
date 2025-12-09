using UnityEngine;
using System.Collections.Generic;
using System;

public class Station : MonoBehaviour
{
    private StationType station;
    public List<GameObject> commuters;
    private Dictionary<int, List<Transform>> connectedStations = new Dictionary<int, List<Transform>>();
    private HashSet<StationType> connectedStationTypes = new HashSet<StationType>();
    private int capacity = 6;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetCapacity()
    {
        return capacity;
    }

    public void IncreaseCapacity()
    {
        capacity = 8;
        transform.localScale = new Vector2 (1.27f, 1.27f);
    }

    public void AddCommuter(GameObject commuter)
    {
        commuters.Add(commuter);
        commuter.transform.position = new Vector3 (Convert.ToSingle(transform.position.x+(.32*(commuters.Count-1))+.6), Convert.ToSingle(transform.position.y+.35), 0);
    }

    public List<GameObject> GetCommuters()
    {
        return commuters;
    }

    public int CommuterSize()
    {
        return commuters.Count;
    }

    public void ClearCommuters()
    {
        foreach (GameObject com in commuters)
        {
            Destroy(com);
        }
        commuters.Clear();
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

    public HashSet<StationType> GetConnections()
    {
        return connectedStationTypes;
    }

    public bool HasConnection(StationType connection)
    {
        return connectedStationTypes.Contains(connection);
    }

    public void SetConnections(int instance, List<Transform> stations)
    {
        if (!connectedStations.ContainsKey(instance))
        {
            connectedStations.Add(instance, stations);
        }
        else if (stations != connectedStations[instance])
        {
            connectedStations[instance] = stations;
        }
        connectedStationTypes.Clear();
        foreach (var i in connectedStations.Keys)
        {
            foreach (Transform t in connectedStations[i])
            {
                if (t != this.transform)
                {
                    connectedStationTypes.Add(t.GetComponent<Station>().GetStationType());
                }
            }
        }
    }
}
