using UnityEngine;

public class TransitLine : MonoBehaviour
{
    private LineRenderer lr;
    public Transform[] stations;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    public void lineSetup(Transform[] points)
    {
        lr.positionCount = points.Length;
        stations = points;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < stations.Length; i++)
        {
            lr.SetPosition(i, stations[i].position);
        }
    }
}
