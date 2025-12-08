using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Station;
using static Unity.Burst.Intrinsics.X86.Avx;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject lineObject;
    [SerializeField] private TextMeshProUGUI getmoneyObject;
    [SerializeField] private GameObject riverObject;
    [SerializeField] private GameObject commuterObject;
    [SerializeField] private GameObject stationObject;
    [SerializeField] private TextMeshProUGUI stationText;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI pauseText;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private Sprite pause;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private Sprite play;
    [SerializeField] private GameObject shop;
    [SerializeField] private GameObject bar;
    [SerializeField] private GameObject restartButton;
    public static GameManager instance;

    private int totalCommuters = 0;
    private List<GameObject> transitStations = new List<GameObject>();
    private float spawnRadius = 1.88f;
    private int maxAttempts = 100;
    private double cash = 0;
    private int riverCurvePoints = 7;
    private LineRenderer riverLR;
    private static Vector3[] riverPoints;
    private int tunnelsUsedInCurrentLine = 0;

    private Transform mousePos;
    private List<Transform> stations = new List<Transform>();
    private HashSet<StationType> stationTypes = new HashSet<StationType>();
    private GameObject previewLine;
    private TransitLine previewTransit;
    private List<UnityEngine.Color> colors = new List<UnityEngine.Color> { UnityEngine.Color.red, UnityEngine.Color.blue, UnityEngine.Color.yellow };
    private bool addedColor = false;
    private List<GameObject> lines = new List<GameObject>();
    private bool isDrawing = false;
    private int maxLines = 3;
    public static bool paused = false;
    private bool shopOpen = false;
    private BottomBar bottomBar;
    private int trains = 0;
    private int upgrades = 0;
    private static int tunnels = 3;

    private bool isEditingLine = false;
    private TransitLine editingTransitLine;
    private int editingSegmentIndex = -1;
    private List<Transform> editingEndStations = new List<Transform>();

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        bottomBar = bar.GetComponent<BottomBar>();
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
            float minY = cam.ViewportToWorldPoint(new Vector3(0, 0, z)).y + 2f;
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
        bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
    }

    void Update()
    {
        Vector3 mouseV3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseV3.z = 0f;

        if (mouseV3.y < 0.2 && mouseV3.x < 5.75 && shopOpen)
        {
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
                        pauseOverlay.GetComponent<SpriteRenderer>().enabled = paused;
                        if (paused)
                        {
                            pauseText.GetComponent<TextMeshProUGUI>().text = "Paused";
                        }
                        pauseText.GetComponent<TextMeshProUGUI>().enabled = paused;
                        return;
                    case "Shop":
                        StartCoroutine(OpenCloseShop());
                        return;
                    case "LineButton":
                        if (maxLines < 4 && cash > 149)
                        {
                            BuyColor();
                            cash -= 150;
                            MoneyAnimation(cashText, -150);
                            bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                        }
                        return;
                    case "TrainButton":
                        if (cash > 109)
                        {
                            trains++;
                            cash -= 110;
                            MoneyAnimation(cashText, -110);
                            bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                        }
                        return;
                    case "StationButton":
                        if (cash > 89)
                        {
                            upgrades++;
                            cash -= 90;
                            MoneyAnimation(cashText, -90);
                            bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                        }
                        return;
                    case "TunnelButton":
                        if (cash > 69)
                        {
                            tunnels++;
                            cash -= 70;
                            MoneyAnimation(cashText, -70);
                            bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                        }
                        return;
                    case "Restart":
                        SceneManager.LoadScene(1);
                        return;
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            HandleLineSegmentDeletion(mouseV3);
            return;
        }

        if (isEditingLine && Input.GetMouseButton(0))
        {
            HandleLineEditing(mouseV3);
            return;
        }

        if (isEditingLine && Input.GetMouseButtonUp(0))
        {
            FinishLineEditing();
            return;
        }

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
                stationTypes.Clear();
                tunnelsUsedInCurrentLine = 0;
                stations.Add(startHit.collider.transform);
                stationTypes.Add(startHit.collider.GetComponent<Station>().GetStationType());
                previewLine = Instantiate(lineObject);
                previewTransit = previewLine.GetComponent<TransitLine>();
                previewTransit.SetColor(colors[0]);
                previewTransit.EnablePreview(stations, mousePos);
                return;
            }
        }

        if (!isDrawing && Input.GetMouseButtonDown(0))
        {
            if (TryStartLineEditing(mouseV3))
            {
                return;
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
                    if (stations[stations.Count-1] != hitStation)
                    {
                        if (stations.Count > 0)
                        {
                            Vector3 lastPos = stations[stations.Count - 1].position;
                            Vector3 newPos = hitStation.position;
                            if (DoesSegmentCrossRiver(lastPos, newPos))
                            {
                                if (tunnelsUsedInCurrentLine >= tunnels)
                                {
                                    return;
                                }
                                tunnelsUsedInCurrentLine++;
                            }
                        }

                        stations.Add(hitStation);
                        stationTypes.Add(mouseHit.collider.GetComponent<Station>().GetStationType());
                    }
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

            if (stations.Count >= 2 && lines.Count < maxLines)
            {
                if (tunnelsUsedInCurrentLine > tunnels)
                {
                    stations.Clear();
                    stationTypes.Clear();
                    tunnelsUsedInCurrentLine = 0;
                    return;
                }

                int nextIndex = lines.Count;
                GameObject newLine = Instantiate(lineObject);
                lines.Add(newLine);

                TransitLine line = newLine.GetComponent<TransitLine>();
                line.SetColor(colors[0]);
                colors.RemoveAt(0);
                line.LineSetup(new List<Transform>(stations), new HashSet<StationType>(stationTypes), tunnelsUsedInCurrentLine);
                tunnels -= tunnelsUsedInCurrentLine;
                bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
            }

            stations.Clear();
            stationTypes.Clear();
            tunnelsUsedInCurrentLine = 0;
        }
    }

    private bool TryStartLineEditing(Vector3 mousePos)
    {
        float minDistance = 0.3f;

        foreach (GameObject lineObj in lines)
        {
            TransitLine transitLine = lineObj.GetComponent<TransitLine>();
            float distance;
            int segmentIndex = transitLine.GetSegmentIndexAtPosition(mousePos, out distance);

            if (segmentIndex >= 0 && distance < minDistance)
            {
                isEditingLine = true;
                editingTransitLine = transitLine;
                editingSegmentIndex = segmentIndex;
                stations.Clear();
                stationTypes.Clear();
                editingEndStations.Clear();
                tunnelsUsedInCurrentLine = 0;

                editingTransitLine.gameObject.SetActive(false);

                List<Transform> originalStations = transitLine.GetStations();

                previewLine = Instantiate(lineObject);
                previewTransit = previewLine.GetComponent<TransitLine>();
                previewTransit.SetColor(transitLine.GetColor());

                for (int i = 0; i <= segmentIndex; i++)
                {
                    stations.Add(originalStations[i]);
                    stationTypes.Add(originalStations[i].GetComponent<Station>().GetStationType());
                }

                for (int i = segmentIndex + 1; i < originalStations.Count; i++)
                {
                    editingEndStations.Add(originalStations[i]);
                }

                this.mousePos.position = mousePos;

                previewTransit.EnableSplitPreview(stations, editingEndStations, this.mousePos);

                return true;
            }
        }

        return false;
    }

    private void HandleLineEditing(Vector3 mouseV3)
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
                if (stations.Count > 0)
                {
                    Vector3 lastPos = stations[stations.Count - 1].position;
                    Vector3 newPos = hitStation.position;

                    if (DoesSegmentCrossRiver(lastPos, newPos))
                    {
                        if (tunnelsUsedInCurrentLine >= tunnels)
                        {
                            return;
                        }
                        tunnelsUsedInCurrentLine++;
                    }
                }

                stations.Add(hitStation);
                stationTypes.Add(mouseHit.collider.GetComponent<Station>().GetStationType());
            }
        }

        if (previewTransit != null)
        {
            previewTransit.UpdateSplitPreview(stations, editingEndStations, mousePos);
        }
    }

    private void FinishLineEditing()
    {
        if (previewTransit != null)
        {
            previewTransit.DisablePreview();
            Destroy(previewLine);
            previewLine = null;
        }

        if (editingTransitLine != null)
        {
            editingTransitLine.gameObject.SetActive(true);
        }

        int originalStationCount = editingSegmentIndex + 1;

        if (stations.Count > originalStationCount)
        {
            if (tunnelsUsedInCurrentLine > tunnels)
            {
                isEditingLine = false;
                editingTransitLine = null;
                editingSegmentIndex = -1;
                stations.Clear();
                stationTypes.Clear();
                editingEndStations.Clear();
                tunnelsUsedInCurrentLine = 0;
                return;
            }

            List<Transform> newStations = new List<Transform>();
            for (int i = originalStationCount; i < stations.Count; i++)
            {
                newStations.Add(stations[i]);
            }

            HashSet<StationType> newTypes = new HashSet<StationType>();
            foreach (var station in newStations)
            {
                newTypes.Add(station.GetComponent<Station>().GetStationType());
            }

            editingTransitLine.InsertStationsAt(editingSegmentIndex + 1, newStations, newTypes);
            editingTransitLine.SetTunnels(tunnelsUsedInCurrentLine);
            tunnels -= tunnelsUsedInCurrentLine;
            bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
        }

        isEditingLine = false;
        editingTransitLine = null;
        editingSegmentIndex = -1;
        stations.Clear();
        stationTypes.Clear();
        editingEndStations.Clear();
        tunnelsUsedInCurrentLine = 0;
    }

    private void HandleLineSegmentDeletion(Vector3 mousePos)
    {
        float minDistance = 0.3f;

        foreach (GameObject lineObj in lines)
        {
            TransitLine transitLine = lineObj.GetComponent<TransitLine>();
            float distance;
            int segmentIndex = transitLine.GetSegmentIndexAtPosition(mousePos, out distance);

            if (segmentIndex >= 0 && distance < minDistance)
            {
                List<Transform> lineStations = transitLine.GetStations();

                bool isStartSegment = segmentIndex == 0;
                bool isEndSegment = segmentIndex == lineStations.Count - 2;

                if (isStartSegment)
                {
                    if (DoesSegmentCrossRiver(lineStations[segmentIndex].position, lineStations[segmentIndex + 1].position))
                    {
                        RefundTunnels(1);
                        transitLine.RemoveTunnel();
                        bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                    }
                    transitLine.RemoveStationRange(0, 1);

                    if (transitLine.GetStations().Count < 2)
                    {
                        colors.Insert(0, lineObj.GetComponent<TransitLine>().GetColor());
                        lines.Remove(lineObj);
                        Destroy(lineObj);
                        bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                    }
                    return;
                }
                else if (isEndSegment)
                {
                    if (DoesSegmentCrossRiver(lineStations[segmentIndex].position, lineStations[segmentIndex + 1].position))
                    {
                        RefundTunnels(1);
                        transitLine.RemoveTunnel();
                        bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                    }
                    transitLine.RemoveStationRange(lineStations.Count - 1, 1);

                    if (transitLine.GetStations().Count < 2)
                    {
                        lines.Remove(lineObj);
                        Destroy(lineObj);
                        bottomBar.UpdateBar(trains, upgrades, tunnels, colors, maxLines);
                    }
                    return;
                }
            }
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
            Vector3 candidatePos = new Vector3(Random.Range(-7.9f, 7.9f), Random.Range(-2f, 4f));
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
            restartButton.SetActive(true);
            paused = true;
            pauseOverlay.GetComponent<SpriteRenderer>().enabled = paused;
            pauseText.GetComponent<TextMeshProUGUI>().text = "You Win";
            pauseText.GetComponent<TextMeshProUGUI>().enabled = true;
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
            Vector3 candidatePos = new Vector3(Random.Range(-7.9f, 7.9f), Random.Range(-2f, 4f));
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
        if (targetStation.GetComponent<Station>().GetCapacity() < targetStation.GetComponent<Station>().CommuterSize())
        {
            paused = true;
            restartButton.SetActive(true);
            pauseOverlay.GetComponent<SpriteRenderer>().enabled = paused;
            pauseText.GetComponent<TextMeshProUGUI>().text = "Game Over";
            pauseText.GetComponent<TextMeshProUGUI>().enabled = true;
        }

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
            startPos = rect.anchoredPosition + Vector2.up * 35f;
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
    private IEnumerator OpenCloseShop()
    {
        Vector3 startPos;
        Vector3 endPos;
        startPos = shop.transform.position;
        float duration = .3f;
        float elapsed = 0f;
        if (!shopOpen)
        {
            endPos = startPos + Vector3.up * 4f;
        }
        else
        {
            endPos = startPos + Vector3.down * 4f;
        }
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            shop.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        shopOpen = !shopOpen;
    }

    void BuyColor()
    {
        if (!addedColor)
        {
            colors.Add(UnityEngine.Color.magenta);
            maxLines++;
            addedColor = true;
        } else
        {
            colors.Add(UnityEngine.Color.green);
            maxLines++;
        }
    }

    public static void RefundTunnels(int refund)
    {
        tunnels += refund;
    }

    private bool DoesSegmentCrossRiver(Vector3 start, Vector3 end)
    {
        for (int i = 0; i < riverPoints.Length - 1; i++)
        {
            if (DoLineSegmentsIntersect(start, end, riverPoints[i], riverPoints[i + 1]))
            {
                return true;
            }
        }
        return false;
    }

    /*
     ChatGPT used to generate math for method
     */
    private bool DoLineSegmentsIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {

        float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (Mathf.Abs(d) < 0.0001f)
        {
            return false;
        }

        float t = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        float u = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            return true;
        }

        return false;
    }
}