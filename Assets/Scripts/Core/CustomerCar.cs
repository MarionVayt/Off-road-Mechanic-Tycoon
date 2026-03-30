using UnityEngine;

public class CustomerCar : Vehicle
{
    public CustomerCar(CarData data)
    {
        Brand = data.Brand;
        Model = data.Model;
        ClassMultiplier = data.ClassMultiplier;
        IsAutomaticTransmission = data.IsAutomaticTransmission;
        CarSprite = data.CarImage;
        
        ConditionOfEngine = Random.Range(10f, 100f);
        ConditionOfRunningGear = Random.Range(10f, 100f);
        ConditionOfBody =  Random.Range(10f, 100f);
        ConditionOfElectronic =  Random.Range(10f, 100f);
    }
}
