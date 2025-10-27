using UnityEngine;
using System.Collections.Generic;

public class Train : MonoBehaviour
{
    private List<Transform> stations;
    private SpriteRenderer sr;
    int stationIndex = 0;
    [SerializeField] float speed = 10f;
    private bool stopped = false;
    private Transform target;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!stopped)
        {
            float movement = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, movement);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.transform == target)
        {
            if (stationIndex < stations.Count)
            {
                stationIndex++;
                stopped = true;
                target = stations[stationIndex];
            }
        }
    }

    public void UpdateTrainLine(List<Transform> line, Color color)
    {
        stations = line;
        sr.color = color;
        stationIndex = 1;
        target = stations[stationIndex];
    }
}
