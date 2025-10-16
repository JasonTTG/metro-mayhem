using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject lineObject;
    [SerializeField] private GameObject stationObject;

    private List<Transform> stations = new List<Transform>();
    private Transform mousePos;
    private bool isDrawing = false;
    private GameObject previewLine;
    private TransitLine previewTransit;
    private float spawnRadius = 1.88f;
    private int maxAttempts = 100;

    void Start()
    {
        StartCoroutine(StationLoop());

        mousePos = new GameObject("MousePosition").transform;

        SpawnStation();
        SpawnStation();
        SpawnStation();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D startHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (startHit.collider && startHit.collider.CompareTag("Station"))
            {
                isDrawing = true;
                stations.Clear();
                stations.Add(startHit.collider.transform);

                previewLine = Instantiate(lineObject);
                previewTransit = previewLine.GetComponent<TransitLine>();
                previewTransit.EnablePreview(stations, mousePos);
            }
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            Vector3 mouseV3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseV3.z = 0f;
            mousePos.position = mouseV3;

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

            if (stations.Count > 1)
            {
                GameObject newLine = Instantiate(lineObject);
                newLine.GetComponent<TransitLine>().LineSetup(stations);
            }

            if (previewTransit != null)
            {
                previewTransit.DisablePreview();
                Destroy(previewLine);
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
    }
}
