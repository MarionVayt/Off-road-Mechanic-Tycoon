using UnityEngine;

[System.Serializable]
public class Order
{
    public Vehicle Car;
    public string ClientStory;
    public float TimeLeft;
    public float MaxTime;

    public Order(Vehicle car, string story, float time)
    {
        Car = car;
        ClientStory = story;
        TimeLeft = time;
        MaxTime = time;
    }
}
