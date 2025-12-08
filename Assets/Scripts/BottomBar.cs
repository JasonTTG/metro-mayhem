using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BottomBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TrainText;
    [SerializeField] private TextMeshProUGUI UpgradeText;
    [SerializeField] private TextMeshProUGUI TunnelText;

    [SerializeField] private GameObject LineRed;
    [SerializeField] private GameObject LineBlue;
    [SerializeField] private GameObject LineYellow;
    [SerializeField] private GameObject LineMagenta;
    [SerializeField] private GameObject LineGreen;

    [SerializeField] private GameObject MagentaLock;
    [SerializeField] private GameObject GreenLock;

    public void UpdateBar(int trains, int upgrades, int tunnels, List<Color> colors, int maxLines)
    {
        Dictionary<GameObject, Color> lineColorMap = new Dictionary<GameObject, Color>
        {
            { LineRed, Color.red },
            { LineBlue, Color.blue },
            { LineYellow, Color.yellow },
            { LineMagenta, Color.magenta },
            { LineGreen, Color.green }
        };

        foreach (var color in lineColorMap)
        {
            SpriteRenderer line = color.Key.GetComponent<SpriteRenderer>();
            Color lineColor = color.Value;
            if (colors.Contains(lineColor)){
                line.color = lineColor;
            } else
            {
                line.color = Color.grey;
            }
        }

        if (maxLines > 4)
        {
            GreenLock.SetActive(false);
        }
        else if (maxLines > 3)
        {
            MagentaLock.SetActive(false);
            LineGreen.GetComponent<SpriteRenderer>().color = Color.black;
        }
        else
        {
            LineGreen.GetComponent<SpriteRenderer>().color = Color.black;
            LineMagenta.GetComponent<SpriteRenderer>().color = Color.black;
        }

        TrainText.text = ":" + trains;
        UpgradeText.text = ":" + upgrades;
        TunnelText.text = ":" + tunnels;
    }
}
