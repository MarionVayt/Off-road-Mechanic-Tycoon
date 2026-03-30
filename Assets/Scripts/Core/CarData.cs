using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Garage Tycoon/Car Data", order = 51)]
public class CarData : ScriptableObject
{
    [Header("Основна інформація")] 
    public string Brand;
    public string Model;
    public bool IsAutomaticTransmission;
    
    [Header("Візуал")]
    public Sprite CarImage;
    
    [Header("Економіка")]
    public float ClassMultiplier = 1.0f;
}
