using System.Collections.Generic;
using UnityEngine;

public class TransitLine : MonoBehaviour
{
    public LineRenderer lr;
    private HashSet<StationType> stationTypes;
    private List<Transform> stations;
    private bool liveUpdating = false;
    private Transform mousePos;
    [SerializeField] GameObject trainObject;
    Color color = Color.white;
    private LineRenderer solidLR;
    private LineRenderer dottedLR;
    private List<Train> trains = new List<Train>();
    private List<Transform> endStations = new List<Transform>();
    private GameObject ghostLineObject;
    private LineRenderer ghostLR;
    private List<Transform> oldStations = new List<Transform>();
    private int tunnels;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (liveUpdating && stations != null)
        {
            if (IsInSplitPreviewMode())
            {
                return;
            }

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

            if (mousePos != null)
            {
                Vector3 mouse = mousePos.position;
                mouse.z = 0f;
                lr.SetPosition(stations.Count, mouse);
            }
        }

        if (ghostLineObject != null && AllTrainsOnNewRoute())
        {
            Destroy(ghostLineObject);
            ghostLineObject = null;
            oldStations.Clear();
        }
    }

    public void LineSetup(List<Transform> points, HashSet<StationType> types, int tunnel)
    {
        stations = new List<Transform>();
        stationTypes = types;
        tunnels = tunnel;

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
            stations[i].GetComponent<Station>().SetConnections(GetInstanceID(), new List<Transform>(stations));
        }

        AddTrain();
    }

    public void AddTrain()
    {
        GameObject newTrainObj = Instantiate(trainObject, stations[0].position, Quaternion.identity);
        Train newTrain = newTrainObj.GetComponent<Train>();

        float startOffset = trains.Count * 0.1f;

        newTrain.UpdateTrainLine(stations, color, stationTypes, startOffset);
        trains.Add(newTrain);
        GameManager.AddTrain(newTrain);
    }

    public void RemoveTrain(Train train)
    {
        if (trains.Contains(train))
        {
            trains.Remove(train);
            GameManager.RemoveTrain(train);
            Destroy(train.gameObject);
        }
    }

    public void RemoveAllTrains()
    {
        for (int i = 0; i < trains.Count; i++)
        {
            if (trains[i] != null)
            {
                RemoveTrain(trains[i]);
            }
        }
        trains.Clear();
    }

    public int GetTrainCount()
    {
        return trains.Count;
    }

    public List<Train> GetTrains()
    {
        return new List<Train>(trains);
    }

    public void EnablePreview(List<Transform> points, Transform mouse)
    {
        stations = points;
        mousePos = mouse;
        liveUpdating = true;
    }

    public void EnableSplitPreview(List<Transform> startPoints, List<Transform> endPoints, Transform mouse)
    {
        stations = new List<Transform>(startPoints);
        endStations = new List<Transform>(endPoints);
        mousePos = mouse;
        liveUpdating = true;
        UpdateSplitPreview(startPoints, endPoints, mouse);
    }

    public void UpdateSplitPreview(List<Transform> startPoints, List<Transform> endPoints, Transform mouse)
    {
        stations = new List<Transform>(startPoints);
        endStations = new List<Transform>(endPoints);
        mousePos = mouse;

        int totalCount = stations.Count + 1 + endStations.Count;
        lr.positionCount = totalCount;

        int index = 0;

        for (int i = 0; i < stations.Count; i++)
        {
            Vector3 pos = stations[i].position;
            pos.z = 0f;
            lr.SetPosition(index++, pos);
        }

        if (mousePos != null)
        {
            Vector3 mousePosition = mousePos.position;
            mousePosition.z = 0f;
            lr.SetPosition(index++, mousePosition);
        }

        for (int i = 0; i < endStations.Count; i++)
        {
            Vector3 pos = endStations[i].position;
            pos.z = 0f;
            lr.SetPosition(index++, pos);
        }
    }

    public bool IsInSplitPreviewMode()
    {
        return liveUpdating && endStations != null && endStations.Count > 0;
    }

    public void DisablePreview()
    {
        liveUpdating = false;
        mousePos = null;
        endStations.Clear();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        lr.startColor = newColor;
        lr.endColor = newColor;

        foreach (Train train in trains)
        {
            if (train != null)
            {
                train.UpdateColor(newColor);
            }
        }
    }

    public void InsertStationsAt(int insertIndex, List<Transform> newStations, HashSet<StationType> newTypes)
    {
        SaveOldRoute();
        stations.InsertRange(insertIndex, newStations);

        foreach (var type in newTypes)
        {
            stationTypes.Add(type);
        }

        UpdateLineRenderer();
        UpdateAllTrains();
        CreateGhostLine();
    }

    public void RemoveStationRange(int startIndex, int count)
    {
        if (startIndex < 0 || startIndex + count > stations.Count)
        {
            return;
        }

        SaveOldRoute();
        stations.RemoveRange(startIndex, count);
        RecalculateStationTypes();
        UpdateLineRenderer();
        UpdateAllTrains();
        CreateGhostLine();
    }

    private void UpdateLineRenderer()
    {
        lr.positionCount = stations.Count;
        for (int i = 0; i < stations.Count; i++)
        {
            lr.SetPosition(i, stations[i].position);
        }
    }

    private void UpdateAllTrains()
    {
        for (int i = 0; i < trains.Count; i++)
        {
            if (trains[i] != null)
            {
                float startOffset = i * 0.1f;
                trains[i].UpdateTrainLine(stations, color, stationTypes, startOffset);
            }
        }
    }

    private void RecalculateStationTypes()
    {
        stationTypes.Clear();
        foreach (Transform station in stations)
        {
            if (station != null)
            {
                Station stationComponent = station.GetComponent<Station>();
                if (stationComponent != null)
                {
                    stationTypes.Add(stationComponent.GetStationType());
                }
            }
        }
    }

    public int GetSegmentIndexAtPosition(Vector3 worldPos, out float distance)
    {
        distance = float.MaxValue;
        int closestSegment = -1;

        for (int i = 0; i < stations.Count - 1; i++)
        {
            float dist = DistancePointToLineSegment(worldPos, stations[i].position, stations[i + 1].position);
            if (dist < distance)
            {
                distance = dist;
                closestSegment = i;
            }
        }

        return closestSegment;
    }

    private float DistancePointToLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

    public List<Transform> GetStations()
    {
        return stations;
    }

    public Color GetColor()
    {
        return color;
    }

    public bool IsEndStation(int stationIndex)
    {
        return stationIndex == 0 || stationIndex == stations.Count - 1;
    }

    void OnDestroy()
    {
        RemoveAllTrains();
        GameManager.RefundTunnels(tunnels);
        if (ghostLineObject != null)
        {
            Destroy(ghostLineObject);
        }
    }

    public void SetTunnels(int amt)
    {
        tunnels = amt;
    }

    public void RemoveTunnel()
    {
        tunnels -= 1;
    }

    private void SaveOldRoute()
    {
        oldStations = new List<Transform>(stations);
    }

    private void CreateGhostLine()
    {
        if (ghostLineObject != null)
        {
            Destroy(ghostLineObject);
        }

        if (oldStations.Count < 2)
            return;

        ghostLineObject = new GameObject("GhostLine");
        ghostLineObject.transform.SetParent(transform);
        ghostLR = ghostLineObject.AddComponent<LineRenderer>();

        ghostLR.material = lr.material;
        ghostLR.startWidth = lr.startWidth;
        ghostLR.endWidth = lr.endWidth;
        ghostLR.sortingLayerName = lr.sortingLayerName;
        ghostLR.sortingOrder = lr.sortingOrder - 1; 

        Color ghostColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.5f);
        ghostLR.startColor = ghostColor;
        ghostLR.endColor = ghostColor;

        ghostLR.positionCount = oldStations.Count;
        for (int i = 0; i < oldStations.Count; i++)
        {
            if (oldStations[i] != null)
            {
                Vector3 pos = oldStations[i].position;
                pos.z = 0f;
                ghostLR.SetPosition(i, pos);
            }
        }
    }

    private bool AllTrainsOnNewRoute()
    {
        foreach (Train train in trains)
        {
            if (train != null && !train.IsOnNewRoute())
            {
                return false;
            }
        }
        return true;
    }
}