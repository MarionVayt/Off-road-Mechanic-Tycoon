using UnityEngine;

public interface IPayout
{
    float CalculateFinalPayout();
}

public class BasePayout : IPayout
{
    private float _amount;
    public BasePayout(float amount) { _amount = amount; }
    
    public void AddMoney(float amount) { _amount += amount; }
    
    public float CalculateFinalPayout() { return _amount; }
}

public class CarWashDecorator : IPayout
{
    private IPayout _wrappedPayout;
    public CarWashDecorator(IPayout wrapped) { _wrappedPayout = wrapped; }
    
    public float CalculateFinalPayout() 
    { 
        return _wrappedPayout.CalculateFinalPayout() + 50f; 
    }
}

public class InteriorCleaningDecorator : IPayout
{
    private IPayout _wrappedPayout;
    public InteriorCleaningDecorator(IPayout wrapped) { _wrappedPayout = wrapped; }
    
    public float CalculateFinalPayout() 
    { 
        return _wrappedPayout.CalculateFinalPayout() + 120f; 
    }
}