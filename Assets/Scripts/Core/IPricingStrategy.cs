using UnityEngine;

public interface IPricingStrategy
{
    float CalculatePayout(float basePayout, float classMultiplier);
}

public class StandardPricingStrategy : IPricingStrategy
{
    public float CalculatePayout(float basePayout, float multiplier)
    {
        return basePayout * multiplier;
    }
}

public class VIPPricingStrategy : IPricingStrategy
{
    public float CalculatePayout(float basePayout, float multiplier)
    {
        return basePayout * multiplier * 2.0f; 
    }
}