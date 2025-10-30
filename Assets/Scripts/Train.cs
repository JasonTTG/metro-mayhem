using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class Train : MonoBehaviour
{
    [SerializeField] private GameObject commuterObject;

    private List<Transform> stations;
    private SpriteRenderer sr;
    private float speed = 3f;
    private float stopDuration = 2f;
    private List<StationType> commuters = new List<StationType>();

    private int stationIndex = 0;
    private Transform target;
    private bool movingForward = true;
    private bool isLoop = false;
    private bool stopped = false;

    void Start()
    {
    }

    void Update()
    {
        if (stations == null)
        {
            return;
        }
        if (!stopped)
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

        while (stationPeople.Count > 0 && added < slots)
        {
            commuters.Add(stationPeople[0]);
            stationPeople.RemoveAt(0); 
            added++;
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



    private void MoveToNextStation()
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
        stopped = false;
    }

    public void UpdateTrainLine(List<Transform> line, Color color)
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        stations = line;
        sr.color = color;
        stationIndex = 0;
        movingForward = true;
        target = stations[1];
        stopped = false;
        isLoop = stations.Count > 1 && stations[0] == stations[stations.Count - 1];
    }
}
