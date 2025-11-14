using System.Collections.Generic;
using UnityEngine;

public class TransitLine : MonoBehaviour
{
    public LineRenderer lr;
    private List<Transform> stations;
    private bool liveUpdating = false;
    private Transform mousePos;
    [SerializeField] GameObject trainObject;
    Color color = Color.white;
    private LineRenderer solidLR;
    private LineRenderer dottedLR;

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
        if (stations.Count >= 2 && stations[0].position == stations[1].position)
        {
            stations.RemoveAt(1);
        }

        lr.positionCount = stations.Count;
        for (int i = 0; i < stations.Count; i++)
        {
            lr.SetPosition(i, stations[i].position);
        }
        GameObject newTrain = Instantiate(trainObject, stations[0].position, Quaternion.identity);
        newTrain.GetComponent<Train>().UpdateTrainLine(stations, color);
    }

    public void ApplyRiverOverlap()
    {
        Vector3[] pts = new Vector3[lr.positionCount];
        lr.GetPositions(pts);

        List<Vector3> solid = new();
        List<Vector3> dotted = new();

        bool wasDotted = false;

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 p = pts[i];
            bool isDotted = GameManager.IsPositionOnRiver(p, 1f);

            if (isDotted)
            {
                dotted.Add(p);

                if (!wasDotted && solid.Count > 0)
                {
                    dotted.Insert(0, solid[solid.Count - 1]);
                }
            }
            else
            {
                solid.Add(p);

                if (wasDotted && dotted.Count > 0)
                {
                    solid.Add(dotted[dotted.Count - 1]);
                }
            }

            wasDotted = isDotted;
        }

        solidLR.positionCount = solid.Count;
        solidLR.SetPositions(solid.ToArray());

        dottedLR.positionCount = dotted.Count;
        dottedLR.SetPositions(dotted.ToArray());
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

    public void SetColor(Color newColor)
    {
        color = newColor;
        lr.startColor = newColor;
        lr.endColor = newColor;
    }
}
