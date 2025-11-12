using UnityEngine;

[CreateAssetMenu(fileName = "New Planet", menuName = "ScriptableObjects/Planets")]
public class Planets : ScriptableObject
{
    public string Name;
    public string Age;

    [Header("Size")]
    public string Diameter;
    public string Circumference;
    public string Volume;
    public string Mass;

    public bool isSmaller;

    [Header("Orbit")]
    public string OrbitingSpeed;
    public string AverageDistanceToSun;
    public string Year;
    public string Day;

    [Header("Surface")]
    public string Area;
    public string Temperature;
    public string Atmosphere;

    [Header("Figure")]
    public int numberFigure = 1;
    public Sprite figure1;
    public Sprite figure2;

    public string figureDes;
}
