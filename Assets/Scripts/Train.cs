using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;

public class Train : MonoBehaviour
{
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
                }
            }
            UpdateSeats();
        }

        yield return new WaitForSeconds(stopDuration);

        List<GameObject> stationPeople = station.GetCommuters();
        station.ClearCommuters();

        int slots = 5 - commuters.Count;
        int added = 0;

        while (stationPeople.Count > 0 && added < slots)
        {
            var commuterObj = stationPeople[0];
            var commuter = commuterObj.GetComponent<Commuter>();

            if (commuter != null)
            {
                commuters.Add(commuter.type);
                stationPeople.RemoveAt(0);
                Destroy(commuterObj);
                added++;
            }
            else
            {
                stationPeople.RemoveAt(0);
            }
        }

        if (stationPeople.Count > 0)
        {
            foreach (GameObject c in stationPeople)
            {
                if (c != null) station.AddCommuter(c);
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
                string spriteName = type.ToString(); 
                Transform sprite = seatChild.Find(spriteName);
                if (sprite != null)
                {
                    sprite.GetComponent<SpriteRenderer>().enabled = true;
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
