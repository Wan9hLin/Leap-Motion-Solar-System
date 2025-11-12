using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    public void QuitApplication()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
