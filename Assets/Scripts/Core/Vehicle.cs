using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle
{
    public string Brand { get; protected set; }
    public string Model { get; protected set; }
    public float ClassMultiplier { get; protected set; }
    public Sprite CarSprite { get; protected set; }

    public enum CarPart
    {
        Engine,
        RunningGear,
        Body,
        Electronics
    }

    public bool IsAutomaticTransmission { get; protected set; }

    private float _conditionOfEngine;

    public float ConditionOfEngine
    {
        get { return _conditionOfEngine; }
        protected set { _conditionOfEngine = Mathf.Clamp(value, 0f, 100f); }
    }

    private float _conditionOfRunningGear;

    public float ConditionOfRunningGear
    {
        get { return _conditionOfRunningGear; }
        protected set { _conditionOfRunningGear = Mathf.Clamp(value, 0f, 100f); }
    }

    private float _conditionOfBody;

    public float ConditionOfBody
    {
        get { return _conditionOfBody; }
        protected set { _conditionOfBody = Mathf.Clamp(value, 0f, 100f); }
    }

    private float _conditionOfElectronic;

    public float ConditionOfElectronic
    {
        get { return _conditionOfElectronic; }
        protected set { _conditionOfElectronic = Mathf.Clamp(value, 0f, 100f); }
    }

    public void ImproveCondition(CarPart part, float amount)
    {
        switch (part)
        {
            case CarPart.Engine:
                ConditionOfEngine = Mathf.Clamp(ConditionOfEngine + amount, 0f, 100f);
                break;
            case CarPart.RunningGear:
                ConditionOfRunningGear = Mathf.Clamp(ConditionOfRunningGear + amount, 0f, 100f);
                break;
            case CarPart.Body:
                ConditionOfBody = Mathf.Clamp(ConditionOfBody + amount, 0f, 100f);
                break;
            case CarPart.Electronics:
                ConditionOfElectronic = Mathf.Clamp(ConditionOfElectronic + amount, 0f, 100f);
                break;
        }
    }

    public void SetPartConditionToMax(CarPart part)
    {
        switch (part)
        {
            case CarPart.Engine:
                ConditionOfEngine = 100.0f;
                break;
            case CarPart.RunningGear:
                ConditionOfRunningGear = 100.0f;
                break;
            case CarPart.Body:
                ConditionOfBody = 100.0f;
                break;
            case CarPart.Electronics:
                ConditionOfElectronic = 100.0f;
                break;
        }
    }
}