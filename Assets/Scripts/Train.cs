using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class Train : MonoBehaviour
{
    [SerializeField] private GameObject commuterObject;

    private List<Transform> stations;
    private HashSet<StationType> stationTypes;
    private SpriteRenderer sr;
    private float speed = 3f;
    private float stopDuration = 2f;
    private List<StationType> commuters = new List<StationType>();
    private ParticleSystem ps;

    private int stationIndex = 0;
    private Transform target;
    private bool movingForward = true;
    private bool isLoop = false;
    private bool stopped = false;

    private bool isOnNewRoute = true;
    private List<Transform> oldStations;
    private int oldStationIndex = 0;
    private bool oldMovingForward = true;
    private bool usingOldRoute = false;

    void Start()
    {
    }

    void Update()
    {
        if (stations == null)
        {
            return;
        }
        if (!stopped && !GameManager.paused)
        {
            float movement = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, movement);
            Vector3 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (Vector3.Distance(transform.position, target.position) < 0.01f && !stopped)
            {
                StartCoroutine(StopAtStation());
            }
        }
    }

    private IEnumerator StopAtStation()
    {
        stopped = true;
        ps.Stop();

        var station = target.GetComponent<Station>();

        if (commuters.Count > 0)
        {
            for (int i = commuters.Count - 1; i >= 0; i--)
            {
                if (commuters[i] == station.GetStationType())
                {
                    commuters.RemoveAt(i);
                    GameManager.instance.NewCommuter();
                }
            }
            UpdateSeats();
        }

        yield return new WaitForSeconds(stopDuration);

        List<StationType> stationPeople = new List<StationType>();
        foreach (GameObject person in station.GetCommuters())
        {
            stationPeople.Add(person.GetComponent<Commuter>().type);
        }
        station.ClearCommuters();

        int slots = 4 - commuters.Count;
        int added = 0;
        int idx = 0;

        while (stationPeople.Count > 0 && added < slots && stationPeople.Count > idx)
        {
            if (stationTypes.Contains(stationPeople[idx]))
            {
                commuters.Add(stationPeople[idx]);
                stationPeople.RemoveAt(idx);
                added++;
            } else
            {
                idx++;
            }
        }

        if (stationPeople.Count > 0)
        {
            foreach (StationType c in stationPeople)
            {
                GameObject newCommuter = Instantiate(commuterObject);
                newCommuter.GetComponent<Commuter>().SetCommuter(c);
                station.AddCommuter(newCommuter);
            }
        }

        UpdateSeats();
        CheckIfOnNewRoute();
        MoveToNextStation();
    }

    private void UpdateSeats()
    {
        Transform seatsParent = transform.Find("Train_0");

        int seatCount = seatsParent.childCount;

        for (int i = 0; i < seatCount; i++)
        {
            Transform seatChild = seatsParent.GetChild(i);
            SpriteRenderer[] renderers = seatChild.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (var sr in renderers)
            {
                sr.enabled = false;
            }

            if (i < commuters.Count)
            {
                StationType type = commuters[i];
                switch (type)
                {
                    case StationType.Circle:
                        seatChild.transform.Find("Circle_0").GetComponent<SpriteRenderer>().enabled = true;
                        break;
                    case StationType.Square:
                        seatChild.transform.Find("Square_0").GetComponent<SpriteRenderer>().enabled = true;
                        break;
                    case StationType.Triangle:
                        seatChild.transform.Find("Triangle_0").GetComponent<SpriteRenderer>().enabled = true;
                        break;
                }
            }
        }
    }

    public void UpdateColor(Color color)
    {
        sr.color = color;
    }

    private void MoveToNextStation()
    {
        ps.Play();

        if (usingOldRoute && oldStations != null)
        {
            int nextIndex = oldMovingForward ? oldStationIndex + 1 : oldStationIndex - 1;

            if (nextIndex >= oldStations.Count || nextIndex < 0)
            {
                TransitionToNewRoute();
                return;
            }

            oldStationIndex = nextIndex;
            target = oldStations[oldStationIndex];

            CheckForRouteTransition();
        }
        else
        {
            int nextIndex = movingForward ? stationIndex + 1 : stationIndex - 1;

            if (nextIndex >= stations.Count || nextIndex < 0)
            {
                if (isLoop)
                {
                    stationIndex = 0;
                    nextIndex = 1;
                }
                else
                {
                    movingForward = !movingForward;
                    nextIndex = movingForward ? stationIndex + 1 : stationIndex - 1;
                }
            }

            stationIndex = nextIndex;
            target = stations[stationIndex];
        }
        stopped = false;
    }

    private void TransitionToNewRoute()
    {
        usingOldRoute = false;
        isOnNewRoute = true;
        oldStations = null;

        Transform currentTarget = target;
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < stations.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, stations[i].position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }

        stationIndex = closestIndex;

        int nextIndex = movingForward ? stationIndex + 1 : stationIndex - 1;
        if (nextIndex >= 0 && nextIndex < stations.Count)
        {
            stationIndex = nextIndex;
            target = stations[stationIndex];
        }
        else
        {
            target = stations[stationIndex];
        }
    }

    private void CheckForRouteTransition()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i] == target)
            {
                usingOldRoute = false;
                isOnNewRoute = true;
                oldStations = null;
                stationIndex = i;
                return;
            }
        }
    }

    public void UpdateTrainLine(List<Transform> line, Color color, HashSet<StationType> st, float offset)
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        ps = GetComponentInChildren<ParticleSystem>();

        if (stations != null && stations.Count > 0)
        {
            oldStations = new List<Transform>(stations);
            oldStationIndex = stationIndex;
            oldMovingForward = movingForward;
            isOnNewRoute = false;

            bool targetStillExists = false;
            int newTargetIndex = -1;

            for (int i = 0; i < line.Count; i++)
            {
                if (line[i] == target)
                {
                    targetStillExists = true;
                    newTargetIndex = i;
                    break;
                }
            }

            if (targetStillExists)
            {
                usingOldRoute = false;
                stationIndex = newTargetIndex;
            }
            else
            {
                usingOldRoute = true;
            }
        }
        else
        {
            isOnNewRoute = true;
            usingOldRoute = false;
        }

        stations = line;
        sr.color = color;
        stationTypes = st;
        isLoop = stations.Count > 1 && stations[0] == stations[stations.Count - 1];

        if (oldStations == null)
        {
            stationIndex = 0;
            movingForward = true;
            target = stations[1];
            transform.position = Vector3.MoveTowards(transform.position, target.position, offset);
            Vector3 direction = target.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void CheckIfOnNewRoute()
    {
        if (isOnNewRoute)
        {
            return;
        }

        if (usingOldRoute)
        {
            return;
        }

        if (oldStations != null && !usingOldRoute)
        {
            isOnNewRoute = true;
            oldStations = null;
        }
    }

    public bool IsOnNewRoute()
    {
        return isOnNewRoute;
    }

    public void MarkOnNewRoute()
    {
        isOnNewRoute = true;
        oldStations = null;
    }
}