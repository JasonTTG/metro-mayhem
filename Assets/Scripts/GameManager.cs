using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject lineObject;
    [SerializeField] private Transform[] stations;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            while (!Input.GetMouseButtonUp(0))
            {
                int n = 0;
                RaycastHit2D mouseHit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (mouseHit.collider.CompareTag("Station"))
                {
                    stations[n] = mouseHit.collider.transform;
                    stations[n + 1] = ;
                    n++;
                }
            }
            RaycastHit2D mouseFinal = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (mouseFinal.collider.CompareTag("Station"))
            {
                GameObject newLine = Instantiate(lineObject);
                newLine.GetComponent<TransitLine>().lineSetup(stations);
            }
        }
    }
}
