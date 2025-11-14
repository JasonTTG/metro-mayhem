using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Station;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject lineObject;
    [SerializeField] private TextMeshProUGUI getmoneyObject;
    [SerializeField] private GameObject riverObject;
    [SerializeField] private GameObject commuterObject;
    [SerializeField] private GameObject stationObject;
    [SerializeField] private TextMeshProUGUI stationText;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private Sprite pause;
    [SerializeField] private Sprite play;
    public static GameManager instance;

    private int totalCommuters = 0;
    private List<GameObject> transitStations = new List<GameObject>();
    private float spawnRadius = 1.88f;
    private int maxAttempts = 100;
    private double cash = 0;
    private int riverCurvePoints = 7;
    private LineRenderer riverLR;
    private Vector3[] riverPoints;

    private Transform mousePos;
    private List<Transform> stations = new List<Transform>();
    private GameObject previewLine;
    private TransitLine previewTransit;
    private List<UnityEngine.Color> colors = new List<UnityEngine.Color> { UnityEngine.Color.red, UnityEngine.Color.blue, UnityEngine.Color.yellow };
    private List<GameObject> lines = new List<GameObject>();
    private bool isDrawing = false;
    private int maxLines = 3;
    public static bool paused = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartCoroutine(StationLoop());
        StartCoroutine(CommuterLoop());
        GameObject riverInstance = Instantiate(riverObject);
        riverLR = riverInstance.GetComponent<LineRenderer>();

        Camera cam = Camera.main;
        float z = 0f;

        Vector3 startPos = cam.ViewportToWorldPoint(new Vector3(0f, Random.Range(0.2f, 0.8f), z));
        startPos.z = 0f;
        Vector3 endPos = cam.ViewportToWorldPoint(new Vector3(1f, Random.Range(0.2f, 0.8f), z));
        endPos.z = 0f;

        int totalPoints = riverCurvePoints + 2;
        riverPoints = new Vector3[totalPoints];

        riverPoints[0] = startPos;
        riverPoints[totalPoints - 1] = endPos;

        for (int i = 1; i <= riverCurvePoints; i++)
        {
            float t = (float)i / (riverCurvePoints + 1);
            float x = Mathf.Lerp(startPos.x, endPos.x, t);
            float minY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y + 1f;
            float maxY = cam.ViewportToWorldPoint(new Vector3(0, 1, z)).y - 1f;
            float y = Random.Range(minY, maxY);

            riverPoints[i] = new Vector3(x, y, 0f);
        }

        riverLR.positionCount = riverPoints.Length;
        riverLR.SetPositions(riverPoints);

        mousePos = new GameObject("MousePosition").transform;

        SpawnStation(StationType.Circle);
        SpawnStation(StationType.Square);
        SpawnStation(StationType.Triangle);
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

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D startHit = Physics2D.Raycast(mouseV3, Vector2.zero);

            if (startHit.collider)
            {
                string colliderTag = startHit.collider.tag;
                switch (colliderTag)
                {
                    case "Pause":
                        paused = !paused;
                        pauseButton.GetComponent<SpriteRenderer>().sprite = paused ? play : pause;
                        break;
                    case "Shop":
                        break;
                }
            }
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

            if (stations.Count > 2 && lines.Count < maxLines)
            {
                int nextIndex = lines.Count;
                GameObject newLine = Instantiate(lineObject);
                lines.Add(newLine);

                TransitLine line = newLine.GetComponent<TransitLine>();
                line.SetColor(colors[nextIndex]);
                line.LineSetup(new List<Transform>(stations));
                line.ApplyRiverOverlap();
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
            if (!paused)
            {
                SpawnStation();
            }
        }
    }

    IEnumerator CommuterLoop()
    {
        while (true)
        {
            float delay = Random.Range(3.5f, 5.5f);
            yield return new WaitForSeconds(delay);
            if (!paused)
            {
                SpawnCommuter();
            }
        }
    }

    void SpawnStation()
    {
        Vector3 spawnPos = Vector3.zero;
        bool validPosition = false;
        float riverSafetyRadius = 1f;

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

            if (!tooClose && IsPositionOnRiver(candidatePos, riverSafetyRadius))
            {
                tooClose = true;
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

    public static float DistancePointToLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

    public static bool IsPositionOnRiver(Vector3 candidatePos, float minDistance)
    {
        for (int i = 0; i < riverPoints.Length - 1; i++)
        {
            if (DistancePointToLineSegment(candidatePos, riverPoints[i], riverPoints[i + 1]) < minDistance)
                return true; 
        }
        return false;
    }

    void SpawnStation(StationType type)
    {
        Vector3 spawnPos = Vector3.zero;
        bool validPosition = false;
        int stationType = 0;
        float riverSafetyRadius = 1f;

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

            if (!tooClose && IsPositionOnRiver(candidatePos, riverSafetyRadius))
            {
                tooClose = true;
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
        if (type == StationType.Square)
        {
            stationType = 1;
        }
        if (type == StationType.Circle)
        {
            stationType = 0;
        }
        if (type == StationType.Triangle)
        {
            stationType = 2;
        }
        GameObject newStation = Instantiate(stationObject, spawnPos, Quaternion.identity);
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

    public void NewCommuter()
    {
        totalCommuters++;
        cash += 1.75;
        stationText.SetText("Total Commuters: " + totalCommuters);
        cashText.SetText("$" + cash);
        TextMeshProUGUI floatingText = Instantiate(getmoneyObject, cashText.transform.parent);
        StartCoroutine(MoneyAnimation(floatingText, 7.25));
    }

    private IEnumerator MoneyAnimation(TextMeshProUGUI tmp, double val)
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.2f));
        RectTransform rect = tmp.rectTransform;
        Vector3 startPos;
        Vector3 endPos;
        if (val > 0)
        {
            startPos = rect.anchoredPosition;
            endPos = startPos + Vector3.up * 35f;
            tmp.color = UnityEngine.Color.green;
        } else
        {
            startPos = rect.anchoredPosition + Vector2.up * 35f ;
            endPos = startPos + Vector3.down * 35f;
            tmp.color = UnityEngine.Color.red;
        }
        UnityEngine.Color startColor = tmp.color;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

            UnityEngine.Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            tmp.color = newColor;

            yield return null;
        }

        Destroy(tmp.gameObject);
    }
}
