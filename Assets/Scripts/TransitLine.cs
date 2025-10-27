using System.Collections.Generic;
using UnityEngine;

public class TransitLine : MonoBehaviour
{
    private LineRenderer lr;
    private List<Transform> stations;
    private bool liveUpdating = false;
    private Transform mousePos;
    [SerializeField] GameObject trainObject;
    Color color = Color.white;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (liveUpdating && stations != null)
        {
            int count = stations.Count;
            if (mousePos != null)
            {
                count++;
            }

            lr.positionCount = count;

            for (int i = 0; i < stations.Count; i++)
            {
                Vector3 pos = stations[i].position;
                pos.z = 0f; 
                lr.SetPosition(i, pos);
            }

            if (mousePos)
            {
                Vector3 mouse = mousePos.position;
                mouse.z = 0f;
                lr.SetPosition(stations.Count, mouse);
            }

            if (mousePos)
            {
                lr.SetPosition(stations.Count, mousePos.position);
            }
        }
    }

    public void LineSetup(List<Transform> points)
    {
        stations = new List<Transform>();

        foreach (var p in points)
        {
            if (p != null && p != mousePos)
            { 
            stations.Add(p);
            }
        }

        lr.positionCount = stations.Count;
        for (int i = 0; i < stations.Count; i++)
        {
            lr.SetPosition(i, stations[i].position);
        }

        GameObject newTrain = Instantiate(trainObject, transform);
        newTrain.GetComponent<Train>().UpdateTrainLine(stations, color);
    }

    public void EnablePreview(List<Transform> points, Transform mouse)
    {
        stations = points;
        mousePos = mouse;
        liveUpdating = true;
    }

    public void DisablePreview()
    {
        liveUpdating = false;
        mousePos = null;
    }
}
