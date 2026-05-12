using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


[System.Serializable]
public class RepairRowUI
{
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI priceText;  
    public Button repairButton; 
    public Image progressBarImage;
}
[System.Serializable]
public class ShopRowUI
{
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI priceText;
    public Button buyButton;
}

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

    [Header("Головний UI")]
    public TextMeshProUGUI moneyText;
    public Image carDisplayImage;

    [Header("Панель Ремонту (Рядки)")]
    public GameObject repairMenuPanel;
    public RepairRowUI engineRow;
    public RepairRowUI gearRow;
    public RepairRowUI bodyRow;
    public RepairRowUI electroRow;

    [Header("Панель Магазину")]
    public GameObject shopPanel;
    public ShopRowUI toolUpgradeRow;
    public ShopRowUI discountUpgradeRow;

    [Header("Система замовлень")]
    public GameObject orderPanel;
    public Transform orderContainer;
    public GameObject orderPrefab;
    public GameObject notificationBadge;
    
    [Header("Налаштування Економіки та Часу")]
    [Tooltip("Мінімальний час, за який треба прийняти замовлення")]
    public float minOrderTime = 15f;
    [Tooltip("Максимальний час, за який треба прийняти замовлення")]
    public float maxOrderTime = 25f;
    
    [Space]
    [Tooltip("Час між появою нових клієнтів (мін/макс)")]
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 10f;

    [Space]
    [Tooltip("Базова виплата за прийняття авто")]
    public float baseAcceptancePayout = 50f;
    [Tooltip("Множник виплати при видачі авто (напр. 1.5 = +50% прибутку)")]
    public float profitMultiplier = 1.5f;

    [Space]
    [Tooltip("Вартість ремонту при стані < 80%")]
    public float repairCostMajor = 200f;
    [Tooltip("Вартість ремонту при стані > 80%")]
    public float repairCostMinor = 50f;

    [Space]
    [Tooltip("Базовий час ремонту однієї деталі")]
    public float baseRepairTime = 2.5f;
    [Tooltip("Мінімально можливий час ремонту (після апгрейдів)")]
    public float minRepairSpeed = 0.5f;

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
        }
        else
        {
            PlayerMoney = 1000.0f;
        }

        UpdateUI();
        repairMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
        if (notificationBadge != null) notificationBadge.SetActive(false);
    }

    private void Update()
    {
        if (_currentCarInBox == null && !_isRepairing)
        {
            HandleOrderSpawning();
        }
        HandleOrderTimers();
    }


    private void HandleOrderSpawning()
    {
        _nextOrderTimer -= Time.deltaTime;
        if (_nextOrderTimer <= 0f && _activeOrders.Count < 3)
        {
            GenerateNewOrder();
            _nextOrderTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    private void GenerateNewOrder()
    {
        Vehicle newCar = _factory.GetRandomBrokenCar();
        float randomTime = Random.Range(minOrderTime, maxOrderTime);
        Order newOrder = new Order(newCar, _clientStories[Random.Range(0, _clientStories.Length)], randomTime);
        
        _activeOrders.Add(newOrder);
        UpdateOrderUI();
        if (orderPanel != null && !orderPanel.activeSelf) notificationBadge?.SetActive(true);
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

    public void AcceptOrder(Order acceptedOrder)
    {
        if (_currentCarInBox != null || _isRepairing) return;

        _currentCarInBox = acceptedOrder.Car;
        _currentCarPayout = baseAcceptancePayout * _currentCarInBox.ClassMultiplier;
        
        _activeOrders.Clear();
        UpdateOrderUI();
        UpdateUI();
        orderPanel?.SetActive(false);
        OpenRepairMenu();
    }

    public void ToggleOrderPanel()
    {
        if (orderPanel == null) return;
        bool isActive = orderPanel.activeSelf;
        orderPanel.SetActive(!isActive);
        if (!isActive) notificationBadge?.SetActive(false);
    }

    private void UpdateOrderUI()
    {
        foreach (Transform child in orderContainer) Destroy(child.gameObject);
        foreach (Order order in _activeOrders)
        {
            GameObject newCard = Instantiate(orderPrefab, orderContainer);
            newCard.GetComponent<OrderUIElement>().Setup(order, this);
        }
    }

    public void OpenRepairMenu()
    {
        if (_currentCarInBox == null) return;
        UpdateUI();
        repairMenuPanel.SetActive(true);
    }

    public void CloseRepairMenu() => repairMenuPanel.SetActive(false);

    private void UpdateUI()
    {
        moneyText.text = $"Баланс: {PlayerMoney}$";

        if (_currentCarInBox != null)
        {
            carDisplayImage.sprite = _currentCarInBox.CarSprite;
            carDisplayImage.gameObject.SetActive(true);
            
            UpdateRow(engineRow, "ДВИГУН", _currentCarInBox.ConditionOfEngine, Vehicle.CarPart.Engine);
            UpdateRow(gearRow, "ХОДОВА", _currentCarInBox.ConditionOfRunningGear, Vehicle.CarPart.RunningGear);
            UpdateRow(bodyRow, "КУЗОВ", _currentCarInBox.ConditionOfBody, Vehicle.CarPart.Body);
            UpdateRow(electroRow, "ЕЛЕКТРОНІКА", _currentCarInBox.ConditionOfElectronic, Vehicle.CarPart.Electronics);
        }
        else
        {
            carDisplayImage.gameObject.SetActive(false);
        }
    }

    private void UpdateRow(RepairRowUI row, string label, float condition, Vehicle.CarPart part)
    {
        if (row == null) return;
        row.statusText.text = $"{label} - {Mathf.RoundToInt(condition)}%";
        float cost = GetRepairCost(part);
        row.priceText.text = cost > 0 ? $"РЕМОНТ ({cost}$)" : "(ГОТОВО)";
        row.repairButton.interactable = cost > 0; 
        if (row.progressBarImage != null)
            row.progressBarImage.fillAmount = 0f;

        if (row.repairButton != null)
            row.repairButton.interactable = cost > 0;
    }

    public void ProcessCarPart(Vehicle.CarPart part, RepairRowUI row)
    {
        if (_currentCarInBox == null || _isRepairing) return;
        float cost = GetRepairCost(part) * (1.0f - (_discountUpgradeLevel * 0.1f));
        
        if (PlayerMoney >= cost) 
        {
            StartCoroutine(RepairRoutine(part, cost, GetRepairCost(part), row));
        }
    }

    private System.Collections.IEnumerator RepairRoutine(Vehicle.CarPart part, float cost, float baseCost, RepairRowUI row)
    {
        _isRepairing = true;
        
        float totalRepairTime = Mathf.Max(minRepairSpeed, baseRepairTime - (_toolUpgradeLevel * 0.5f));
        float elapsedTime = 0f;

        if (row != null && row.priceText != null)
            row.priceText.text = "ЛАГОДЖУ...";

        if (row != null && row.repairButton != null)
            row.repairButton.interactable = false;

        while (elapsedTime < totalRepairTime)
        {
            elapsedTime += Time.deltaTime;
            
            float fillPercentage = elapsedTime / totalRepairTime;
            
            if (row != null && row.progressBarImage != null)
                row.progressBarImage.fillAmount = fillPercentage;
            
            yield return null; 
        }
        
        if (row != null && row.progressBarImage != null)
            row.progressBarImage.fillAmount = 1f;
            
        PlayerMoney -= cost;
        _currentCarInBox.SetPartConditionToMax(part);
        _currentCarPayout += baseCost * profitMultiplier; 
        
        _isRepairing = false;
        UpdateUI();
        SaveGame();
    }
    
    public void OnRepairEngineBtnClick() => ProcessCarPart(Vehicle.CarPart.Engine, engineRow);
    public void OnRepairGearBtnClick() => ProcessCarPart(Vehicle.CarPart.RunningGear, gearRow);
    public void OnRepairBodyBtnClick() => ProcessCarPart(Vehicle.CarPart.Body, bodyRow);
    public void OnRepairElectroBtnClick() => ProcessCarPart(Vehicle.CarPart.Electronics, electroRow);

    public void FinishWorkAndReturnCar()
    {
        if (_currentCarInBox == null || _isRepairing) return;
        PlayerMoney += _currentCarPayout;
        _currentCarInBox = null;
        _currentCarPayout = 0f;
        UpdateUI();
        repairMenuPanel.SetActive(false);
        SaveGame();
    }

    public float GetRepairCost(Vehicle.CarPart part)
    {
        if (_currentCarInBox == null) return 0f;
        float cond = 0;
        if (part == Vehicle.CarPart.Engine) cond = _currentCarInBox.ConditionOfEngine;
        else if (part == Vehicle.CarPart.RunningGear) cond = _currentCarInBox.ConditionOfRunningGear;
        else if (part == Vehicle.CarPart.Body) cond = _currentCarInBox.ConditionOfBody;
        else if (part == Vehicle.CarPart.Electronics) cond = _currentCarInBox.ConditionOfElectronic;

        if (cond >= 100f) return 0f;
        float price = (cond < 80f ? repairCostMajor : repairCostMinor);
        return price * _currentCarInBox.ClassMultiplier;
    }
    
    public void ToggleShopPanel()
    {
        if (shopPanel == null) return;
        bool isPanelActive = shopPanel.activeSelf;
        if (!isPanelActive)
        {
            UpdateShopUI();
        }
        shopPanel.SetActive(!isPanelActive);
    }
    private void UpdateShopUI()
    {
        float nextToolPrice = 1000f + (_toolUpgradeLevel * 1000f);
        toolUpgradeRow.infoText.text = $"ШВИДКІ ІНСТРУМЕНТИ (Рівень {_toolUpgradeLevel})";
        toolUpgradeRow.priceText.text = $"КУПИТИ ({nextToolPrice}$)";
        
        toolUpgradeRow.buyButton.interactable = PlayerMoney >= nextToolPrice;

        float nextDiscountPrice = 1500f + (_discountUpgradeLevel * 1500f);
        discountUpgradeRow.infoText.text = $"ОПТОВІ ЗАКУПІВЛІ (Рівень {_discountUpgradeLevel})";
        discountUpgradeRow.priceText.text = $"КУПИТИ ({nextDiscountPrice}$)";
        
        discountUpgradeRow.buyButton.interactable = PlayerMoney >= nextDiscountPrice;
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
    private void SaveGame() 
    { 
        SaveData dataToSave = new SaveData();
        dataToSave.MoneyOfPlayer = PlayerMoney;
        dataToSave.toolUpgradeLevel = _toolUpgradeLevel;
        dataToSave.discountUpgradeLevel = _discountUpgradeLevel;
        SaveManager.Save(dataToSave);
    }
}