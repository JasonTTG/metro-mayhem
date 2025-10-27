using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Station;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject lineObject;
    [SerializeField] private GameObject commuterObject;
    [SerializeField] private GameObject stationObject;


    private List<GameObject> transitStations = new List<GameObject>();
    private float spawnRadius = 1.88f;
    private int maxAttempts = 100;

    private Transform mousePos;
    private List<Transform> stations = new List<Transform>();
    private GameObject previewLine;
    private TransitLine previewTransit;
    private List<UnityEngine.Color> colors = new List<UnityEngine.Color> { UnityEngine.Color.red, UnityEngine.Color.blue, UnityEngine.Color.yellow };
    private List<GameObject> lines = new List<GameObject>();
    private bool isDrawing = false;
    private int maxLines = 3;

    void Start()
    {
        StartCoroutine(StationLoop());
        StartCoroutine(CommuterLoop());

        mousePos = new GameObject("MousePosition").transform;

        SpawnStation();
        SpawnStation();
        SpawnStation();
    }

    void Update()
    {
        Vector3 mouseV3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseV3.z = 0f;

        if (lines.Count >= maxLines)
        {
            if (previewLine != null)
            {
                Destroy(previewLine);
                previewLine = null;
            }
            isDrawing = false;
            return;
        }

        if (!isDrawing && Input.GetMouseButtonDown(0))
        {
            RaycastHit2D startHit = Physics2D.Raycast(mouseV3, Vector2.zero);

            if (startHit.collider && startHit.collider.CompareTag("Station"))
            {
                isDrawing = true;
                stations.Clear();
                stations.Add(startHit.collider.transform);

                previewLine = Instantiate(lineObject);
                previewTransit = previewLine.GetComponent<TransitLine>();
                int colorIndex = Mathf.Min(lines.Count, colors.Count - 1);
                previewTransit.SetColor(colors[colorIndex]);
                previewTransit.EnablePreview(stations, mousePos);
            }
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            if (stations.Count > 0)
            {
                Transform lastStation = stations[stations.Count - 1];
                float dist = Vector3.Distance(mouseV3, lastStation.position);
                if (dist > 0.5f)
                {
                    mousePos.position = mouseV3;
                }
                else
                {
                    mousePos.position = lastStation.position;
                }
            }

            RaycastHit2D mouseHit = Physics2D.Raycast(mouseV3, Vector2.zero);
            if (mouseHit.collider && mouseHit.collider.CompareTag("Station"))
            {
                Transform hitStation = mouseHit.collider.transform;

                if (stations.Count < 2 ||
                    (stations[stations.Count - 1] != hitStation && stations[stations.Count - 2] != hitStation))
                {
                    stations.Add(hitStation);
                }
            }
        }

        if (isDrawing && Input.GetMouseButtonUp(0))
        {
            isDrawing = false;

            if (previewTransit != null)
            {
                previewTransit.DisablePreview();
                Destroy(previewLine);
                previewLine = null;
            }

            if (stations.Count > 1 && lines.Count < maxLines)
            {
                int nextIndex = lines.Count;
                GameObject newLine = Instantiate(lineObject);
                lines.Add(newLine);

                TransitLine line = newLine.GetComponent<TransitLine>();
                line.SetColor(colors[nextIndex]);
                line.LineSetup(new List<Transform>(stations));
            }

            stations.Clear();
        }
    }


    IEnumerator StationLoop()
    {
        while (true)
        {
            float delay = Random.Range(25f, 30f);
            yield return new WaitForSeconds(delay);
            SpawnStation();
        }
    }

    IEnumerator CommuterLoop()
    {
        while (true)
        {
            float delay = Random.Range(3.5f, 5.5f);
            yield return new WaitForSeconds(delay);
            SpawnCommuter();
        }
    }

    void SpawnStation()
    {
        Vector3 spawnPos = Vector3.zero;
        bool validPosition = false;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 candidatePos = new Vector3(Random.Range(-7.9f, 7.9f), Random.Range(-4f, 4f));
            Collider2D[] nearby = Physics2D.OverlapCircleAll(candidatePos, spawnRadius);

            bool tooClose = false;
            foreach (Collider2D col in nearby)
            {
                if (col.CompareTag("Station"))
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                spawnPos = candidatePos;
                validPosition = true;
                break;
            }
        }

        if (!validPosition)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        GameObject newStation = Instantiate(stationObject, spawnPos, Quaternion.identity);
        int stationType = Random.Range(0, 3);
        newStation.GetComponent<Station>().SetStation(stationType);
        transitStations.Add(newStation);
    }

    void SpawnCommuter()
    {
        int spawnStation = Random.Range(0, transitStations.Count);
        GameObject targetStation = transitStations[spawnStation];
        StationType[] types = (StationType[])System.Enum.GetValues(typeof(StationType));
        StationType[] filtered = System.Array.FindAll(types, t => t != targetStation.GetComponent<Station>().GetStationType());
        StationType stationType = filtered[Random.Range(0, filtered.Length)];
        GameObject newCommuter = Instantiate(commuterObject);
        newCommuter.GetComponent<Commuter>().SetCommuter(stationType);
        targetStation.GetComponent<Station>().AddCommuter(newCommuter);
    }
}
