using UnityEngine;

[System.Serializable]
public class Order
{
    public Vehicle Car;
    public string ClientStory;
    public float TimeLeft;
    public float MaxTime;

    public IPricingStrategy PricingStrategy; 
    
    public Order(Vehicle car, string story, float time, IPricingStrategy strategy)
    {
        Car = car;
        ClientStory = story;
        TimeLeft = time;
        MaxTime = time;
        PricingStrategy = strategy;
    }
}
