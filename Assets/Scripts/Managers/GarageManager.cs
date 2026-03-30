using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GarageManager : MonoBehaviour
{
    private CarFactory _factory;
    private Vehicle _currentCarInBox;
    public float PlayerMoney { get; private set; }
    private float _currentCarPayout = 0f;
    private bool _isRepairing = false;
    
    [Header("UI Елементи")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI engineConditionText;
    public TextMeshProUGUI runningGearConditionText;
    public TextMeshProUGUI bodyConditionText;
    public TextMeshProUGUI electronicsConditionText;
    public GameObject repairMenuPanel;
    public TextMeshProUGUI enginePriceText;
    public TextMeshProUGUI runningGearPriceText;
    public TextMeshProUGUI bodyPriceText;
    public TextMeshProUGUI electronicsPriceText;
    public Image carDisplayImage;

    private void Start()
    {
        _factory = new CarFactory();
        
        SaveData loadedData = SaveManager.Load();

        if (loadedData != null)
        {
            PlayerMoney = loadedData.MoneyOfPlayer;
            Debug.Log("Гроші успішно завантажено. Баланс: " + PlayerMoney);
        }
        else
        {
            PlayerMoney = 1000.0f;
            Debug.Log("Перший запуск. Видано стартові 1000$");
        }

        Debug.Log("Гараж відкрито! Чекаємо на клієнтів.");
        UpdateUI();
        repairMenuPanel.SetActive(false);
    }

    private void UpdateUI()
    {
        moneyText.text = $"Баланс: {PlayerMoney}$";

        if (_currentCarInBox != null)
        {
            carDisplayImage.sprite = _currentCarInBox.CarSprite;
            carDisplayImage.gameObject.SetActive(true);
            engineConditionText.text = $"Двигун: {Mathf.RoundToInt(_currentCarInBox.ConditionOfEngine)}%";
            runningGearConditionText.text = $"Ходова: {Mathf.RoundToInt(_currentCarInBox.ConditionOfRunningGear)}%";
            bodyConditionText.text = $"Кузов: {Mathf.RoundToInt(_currentCarInBox.ConditionOfBody)}%";
            electronicsConditionText.text = $"Електроніка: {Mathf.RoundToInt(_currentCarInBox.ConditionOfElectronic)}%";
            UpdateRepairPrices();
        }
        else
        {
            carDisplayImage.gameObject.SetActive(false);
            engineConditionText.text = "Двигун: ---";
            runningGearConditionText.text = "Ходова: ---";
            bodyConditionText.text = "Кузов: ---";
            electronicsConditionText.text = "Електроніка: ---";
        }
    }

    public void AcceptNewClient()
    {
        if (_currentCarInBox != null || _isRepairing)
        {
            Debug.LogWarning("Бокс зайнятий! Спочатку відремонтуйте поточну машину.");
            return;
        }

        _currentCarInBox = _factory.GetRandomBrokenCar();
        _currentCarPayout = 50f * _currentCarInBox.ClassMultiplier;
        UpdateUI();
    }

    public void ProcessCarPart(Vehicle.CarPart part)
    {
        if (_currentCarInBox == null || _isRepairing) return; 

        float costOfRepair = GetRepairCost(part);

        if (costOfRepair == 0f) return;

        if (PlayerMoney >= costOfRepair)
        {
            StartCoroutine(RepairRoutine(part, costOfRepair));
        }
        else
        {
            Debug.LogWarning("Недостатньо грошей для ремонту!");
        }
    }

    private System.Collections.IEnumerator RepairRoutine(Vehicle.CarPart part, float costOfRepair)
    {
        _isRepairing =  true;

        yield return new WaitForSeconds(2.0f);
        
        PlayerMoney -= costOfRepair;
        _currentCarInBox.SetPartConditionToMax(part);
        _currentCarPayout += costOfRepair * 1.5f;
        
        _isRepairing = false;

        Debug.Log("Ремонт успішно завершено!");
        
        UpdateUI();
        SaveGame();
    }
    
    public void OnRepairEngineBtnClick()
    {
        ProcessCarPart(Vehicle.CarPart.Engine);
        UpdateUI();
    }
    
    public void OnRepairRunningGearBtnClick()
    {
        ProcessCarPart(Vehicle.CarPart.RunningGear);
        UpdateUI();
    }

    public void OnRepairBodyBtnClick()
    {
        ProcessCarPart(Vehicle.CarPart.Body);
        UpdateUI();
    }

    public void OnRepairElectronicsBtnClick()
    {
        ProcessCarPart(Vehicle.CarPart.Electronics);
        UpdateUI();
    }
    
    public void FinishWorkAndReturnCar()
    {
        if (_currentCarInBox == null || _isRepairing)
        {
            Debug.LogWarning("Бокс і так порожній!");
            return;
        }
        
        float payment = _currentCarPayout; 
        PlayerMoney += payment;

        Debug.Log($"Машину віддано клієнту! Зароблено: {payment}$");

        _currentCarInBox = null;
        _currentCarPayout = 0f;
        
        UpdateUI();
        SaveGame();
    }

    public float GetRepairCost(Vehicle.CarPart part)
    {
        if (_currentCarInBox == null) return 0f; 
        
        float currentCondition = 0f;

        switch (part)
        {
            case Vehicle.CarPart.Engine:
                currentCondition = _currentCarInBox.ConditionOfEngine;
                break;
            case Vehicle.CarPart.RunningGear:
                currentCondition = _currentCarInBox.ConditionOfRunningGear;
                break;
            case Vehicle.CarPart.Body:
                currentCondition = _currentCarInBox.ConditionOfBody;
                break;
            case Vehicle.CarPart.Electronics:
                currentCondition = _currentCarInBox.ConditionOfElectronic;
                break;
        }
        
        if (currentCondition < 80f)
        {
            return 200f * _currentCarInBox.ClassMultiplier;
        }
        else if (currentCondition < 100f)
        {
            return 50f * _currentCarInBox.ClassMultiplier;
        }
        else
        {
            return 0f;
        }
    }

    public void OpenRepairMenu()
    {
        if (_currentCarInBox == null)
        {
            Debug.LogWarning("Немає машини для ремонту!");
            return;
        }

        UpdateRepairPrices();
        repairMenuPanel.SetActive(true);
    }
    public void CloseRepairMenu()
    {
        repairMenuPanel.SetActive(false);
    }
    private void UpdateRepairPrices()
    {
        if (_currentCarInBox == null) return;
        
        enginePriceText.text = $"Двигун: {GetRepairCost(Vehicle.CarPart.Engine)}$";
        runningGearPriceText.text = $"Ходова: {GetRepairCost(Vehicle.CarPart.RunningGear)}$";
        bodyPriceText.text = $"Кузов: {GetRepairCost(Vehicle.CarPart.Body)}$";
        electronicsPriceText.text = $"Електроніка: {GetRepairCost(Vehicle.CarPart.Electronics)}$";
    }

    private void SaveGame()
    {
        SaveData dataToSave = new SaveData();
        dataToSave.MoneyOfPlayer = PlayerMoney;
        SaveManager.Save(dataToSave);
    }
}
