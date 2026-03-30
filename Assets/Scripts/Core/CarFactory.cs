using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarFactory
{
    private CarData[] _availableCars;
    public CarFactory()
    {
        _availableCars = Resources.LoadAll<CarData>("CarsDatabase");

        if (_availableCars.Length == 0)
            Debug.LogError("База машин порожня! Створи картки в папці Resources/CarsDatabase");
        
    }

    public Vehicle GetRandomBrokenCar()
    {
        int randomIndex = Random.Range(0, _availableCars.Length);
        CarData randomCarData = _availableCars[randomIndex];
        return new CustomerCar(randomCarData);
    }
}
