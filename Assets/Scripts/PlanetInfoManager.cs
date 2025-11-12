using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class PlanetInfoManager : MonoBehaviour
{
    public Planets planet;
    public TextMeshPro Name;
    public TextMeshPro Age;

    [Header("Size")]
    public TextMeshPro Diameter;
    public TextMeshPro Circumference;
    public TextMeshPro Volume;
    public TextMeshPro Mass;

    [Header("Orbit")]
    public TextMeshPro OrbitingSpeed;
    public TextMeshPro AverageDistanceToSun;
    public TextMeshPro Year;
    public TextMeshPro Day;

    [Header("Surface")]
    public TextMeshPro Area;
    public TextMeshPro Temperature;
    public TextMeshPro Atmosphere;

    [Header("Figure")]
    public Image Figure1;
    public Image Figure2;

    public TextMeshPro FigureDes;

    public void SetPlanetInfo()
    {
        Name.text = planet.Name;
        Age.text = "Age: " + planet.Age;

        Diameter.text = "Diameter: " + planet.Diameter;
        Circumference.text = "Circumference: " + planet.Circumference;
        Volume.text = "Volume: " + planet.Volume;
        Mass.text = "Mass: " + planet.Mass;

        OrbitingSpeed.text = "Orbiting Speed: " + planet.OrbitingSpeed;
        AverageDistanceToSun.text = "Av. Dist. to Sun: " + planet.AverageDistanceToSun;
        Year.text = "Year: " + planet.Year;
        Day.text = "Day: " + planet.Day;

        Area.text = "Area: " + planet.Area;
        Temperature.text = "Temperature: " + planet.Temperature;
        Atmosphere.text = "Atmosphere: " + planet.Atmosphere;

        Figure1.sprite = planet.figure1;

        if (planet.numberFigure > 1)
        {
            Figure2.sprite = planet.figure2;
        }

        FigureDes.text = planet.figureDes;
    }    
}
