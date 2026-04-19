using System.Collections.Generic;
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
    private int _toolUpgradeLevel = 0;
    private int _discountUpgradeLevel = 0;
    private string[] _clientStories = new string[]
    {
        "Влетів у яму на трасі, подивіться ходову.",
        "Двигун троїть і жере масло. Допоможіть!",
        "Глючить електроніка, фари живуть своїм життям.",
        "Потрібне повне ТО перед продажем авто.",
        "Дружина каже, що щось стукає. Я не чую, але перевірте."
    };
    
    [Header("UI Елементи")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI engineConditionText;
    public TextMeshProUGUI runningGearConditionText;
    public TextMeshProUGUI bodyConditionText;
    public TextMeshProUGUI electronicsConditionText;
    public Image carDisplayImage;
    
    [Header("UI Елементи - Меню ремонту")]
    public GameObject repairMenuPanel;
    public TextMeshProUGUI enginePriceText;
    public TextMeshProUGUI runningGearPriceText;
    public TextMeshProUGUI bodyPriceText;
    public TextMeshProUGUI electronicsPriceText;
    
    [Header("UI Елементи - Магазин")]
    public GameObject shopPanel;
    public TextMeshProUGUI toolUpgradeText;
    public TextMeshProUGUI discountUpgradeText;
    
    [Header("Система замовлень")]
    public GameObject orderPanel;
    public Transform orderContainer;
    public GameObject orderPrefab;
    
    private List<Order> _activeOrders = new List<Order>();
    private float _nextOrderTimer = 0f;

    private void Start()
    {
        _factory = new CarFactory();
        
        SaveData loadedData = SaveManager.Load();

        if (loadedData != null)
        {
            PlayerMoney = loadedData.MoneyOfPlayer;
            _toolUpgradeLevel = loadedData.toolUpgradeLevel;
            _discountUpgradeLevel = loadedData.discountUpgradeLevel;
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
        shopPanel.SetActive(false);
    }
    
    private void Update()
    {
        if (_currentCarInBox == null && !_isRepairing)
        {
            HandleOrderSpawning();
        }

        HandleOrderTimers();
    }
    
    private void GenerateNewOrder()
    {
        Vehicle newCar = _factory.GetRandomBrokenCar();
        
        string randomStory = _clientStories[Random.Range(0, _clientStories.Length)];
        
        float randomTime = Random.Range(15f, 25f);
        
        Order newOrder = new Order(newCar, randomStory, randomTime);
        
        _activeOrders.Add(newOrder);
        
        UpdateOrderUI();
    }

    private void HandleOrderSpawning()
    {
        _nextOrderTimer -= Time.deltaTime;
        if (_nextOrderTimer <= 0f && _activeOrders.Count < 3)
        {
            GenerateNewOrder();
            _nextOrderTimer = Random.Range(5f, 10f);
        }
    }
    
    private void HandleOrderTimers()
    {
        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            _activeOrders[i].TimeLeft -= Time.deltaTime;
            if (_activeOrders[i].TimeLeft <= 0)
            {
                _activeOrders.RemoveAt(i);
                UpdateOrderUI();
            }
        }
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
    
    private void UpdateOrderUI()
    {
        foreach (Transform child in orderContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Order order in _activeOrders)
        {
            GameObject newCard = Instantiate(orderPrefab, orderContainer);
            
            OrderUIElement uiElement = newCard.GetComponent<OrderUIElement>();
            uiElement.Setup(order, this);
        }
    }

    public void AcceptOrder(Order acceptedOrder)
    {
        if (_currentCarInBox != null || _isRepairing) return;
        
        _currentCarInBox = acceptedOrder.Car;
        _currentCarPayout = 50f * _currentCarInBox.ClassMultiplier;
        
        _activeOrders.Remove(acceptedOrder);

        UpdateOrderUI(); 
        UpdateUI();
    }

    public void ProcessCarPart(Vehicle.CarPart part)
    {
        if (_currentCarInBox == null || _isRepairing) return; 

        float baseCost = GetRepairCost(part);
        if (baseCost == 0f) return;
        
        float discountMultiplier = 1.0f - (_discountUpgradeLevel * 0.1f);
        float actualCostToPay = baseCost * discountMultiplier;

        if (PlayerMoney >= actualCostToPay)
        {
            StartCoroutine(RepairRoutine(part, actualCostToPay, baseCost));
        }
        else
        {
            Debug.LogWarning("Недостатньо грошей для ремонту!");
        }
    }

    private System.Collections.IEnumerator RepairRoutine(Vehicle.CarPart part, float actualCostToPay, float baseCost)
    {
        _isRepairing = true;
        
        float repairTime = Mathf.Max(0.5f, 2.5f - (_toolUpgradeLevel * 0.5f)); 
        
        yield return new WaitForSeconds(repairTime); 
        
        PlayerMoney -= actualCostToPay;
        _currentCarInBox.SetPartConditionToMax(part);
        
        _currentCarPayout += baseCost * 1.5f; 

        _isRepairing = false;
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
    
    public void BuyToolUpgrade()
    {
        float upgradePrice = 1000f + (_toolUpgradeLevel * 1000f); 

        if (PlayerMoney >= upgradePrice)
        {
            PlayerMoney -= upgradePrice;
            _toolUpgradeLevel++;
            Debug.Log($"Інструменти прокачано! Поточний рівень: {_toolUpgradeLevel}");
            SaveGame();
            UpdateUI();
            UpdateShopUI();
        }
    }

    public void BuyDiscountUpgrade()
    {
        float upgradePrice = 1500f + (_discountUpgradeLevel * 1500f);

        if (PlayerMoney >= upgradePrice)
        {
            PlayerMoney -= upgradePrice;
            _discountUpgradeLevel++;
            Debug.Log($"Оптові закупівлі прокачано! Поточний рівень: {_discountUpgradeLevel}");
            SaveGame();
            UpdateUI();
            UpdateShopUI();
        }
    }
    
    public void OpenShop()
    {
        UpdateShopUI();
        shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    private void UpdateShopUI()
    {
        float nextToolPrice = 1000f + (_toolUpgradeLevel * 1000f);
        float nextDiscountPrice = 1500f + (_discountUpgradeLevel * 1500f);
        
        toolUpgradeText.text = $"Швидкі інструменти (Рівень {_toolUpgradeLevel})\nЦіна: {nextToolPrice}$";
        discountUpgradeText.text = $"Оптові закупівлі (Рівень {_discountUpgradeLevel})\nЦіна: {nextDiscountPrice}$";
    }

    private void SaveGame()
    {
        SaveData dataToSave = new SaveData();
        dataToSave.MoneyOfPlayer = PlayerMoney;
        dataToSave.toolUpgradeLevel = _toolUpgradeLevel;
        dataToSave.discountUpgradeLevel = _discountUpgradeLevel;
        SaveManager.Save(dataToSave);
    }
}
