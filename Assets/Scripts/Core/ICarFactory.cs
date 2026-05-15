using UnityEngine;

public interface ICarFactory
{
    Vehicle GetRandomBrokenCar();
}

public class CarFactoryLoggerProxy : ICarFactory
{
    private CarFactory _realFactory;

    public CarFactoryLoggerProxy()
    {
        _realFactory = new CarFactory(); 
    }

    public Vehicle GetRandomBrokenCar()
    {
        Vehicle generatedCar = _realFactory.GetRandomBrokenCar();
        
        Debug.Log($"[PROXY SYSTEM] Заїхало нове авто! Клас: множник {generatedCar.ClassMultiplier}. " +
                  $"Стан двигуна: {Mathf.RoundToInt(generatedCar.ConditionOfEngine)}%");
        
        return generatedCar;
    }
}