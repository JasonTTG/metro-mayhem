using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    void OnMouseDown()
    {
        if (gameObject.CompareTag("Start"))
        {
            SceneManager.LoadScene(1);
        }
    }
}
