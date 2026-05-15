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

[System.Serializable]
public class ServiceRowUI
{
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI priceText;
    public Button serviceButton;
    public Image progressBarImage;
}

public class GarageManager : MonoBehaviour
{
    private ICarFactory _factory;
    private Vehicle _currentCarInBox;
    private BasePayout _basePayoutSystem;
    private IPayout _finalPayoutSystem;
    public float PlayerMoney { get; private set; }
    private float _currentCarPayout = 0f;
    private IGarageState _currentState;
    private int _toolUpgradeLevel = 0;
    private int _discountUpgradeLevel = 0;
    public int PlayerLevel { get; private set; } = 1;
    public float PlayerXP { get; private set; } = 0f;

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
    
    [Header("Система Рівнів (UI)")]
    public TextMeshProUGUI levelText;
    public Image xpProgressBar;
    
    [Header("Система сповіщень (Попапи)")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;
    
    [Header("Вкладки меню ремонту")]
    public GameObject repairPage;
    public GameObject servicePage;

    [Header("Панель Ремонту (Рядки)")]
    public GameObject repairMenuPanel;
    public RepairRowUI engineRow;
    public RepairRowUI gearRow;
    public RepairRowUI bodyRow;
    public RepairRowUI electroRow;
    
    [Header("Панель Послуг (Вкладка послуг)")]
    public ServiceRowUI carWashRow;
    public ServiceRowUI interiorCleaningRow;

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
    public float minOrderTime = 15f;
    public float maxOrderTime = 25f;
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 10f;
    public float baseAcceptancePayout = 50f;
    public float profitMultiplier = 1.5f;
    public float repairCostMajor = 200f;
    public float repairCostMinor = 50f;
    public float baseRepairTime = 2.5f;
    public float minRepairSpeed = 0.5f;
    
    [Space]
    [Header("Налаштування Послуг")]
    public float carWashCost = 20f;
    public float interiorCleaningCost = 50f; 
    public float serviceTime = 1.5f;
    private List<Order> _activeOrders = new List<Order>();
    private float _nextOrderTimer = 0f;

    private void Start()
    {
        _currentState = new GarageIdleState();
        
        _factory = new CarFactoryLoggerProxy();
        SaveData loadedData = SaveManager.Load();

        if (loadedData != null)
        {
            PlayerMoney = loadedData.MoneyOfPlayer;
            _toolUpgradeLevel = loadedData.toolUpgradeLevel;
            _discountUpgradeLevel = loadedData.discountUpgradeLevel;
            PlayerLevel = loadedData.playerLevel == 0 ? 1 : loadedData.playerLevel; 
            PlayerXP = loadedData.playerXP;
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
        if (_currentState is GarageIdleState)
        {
            HandleOrderSpawning();
        }
        HandleOrderTimers();
    }
    
    private float GetXPForNextLevel()
    {
        return 100f * PlayerLevel * 1.5f; 
    }
    
    private void AddXP(float amount)
    {
        PlayerXP += amount;

        while (PlayerXP >= GetXPForNextLevel())
        {
            PlayerXP -= GetXPForNextLevel();
            PlayerLevel++;
            Debug.Log($"РІВЕНЬ ПІДВИЩЕНО! Тепер ти механік {PlayerLevel} рівня!");
            
            PlayerMoney += 500f * PlayerLevel;
        }
        UpdateUI();
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
        
        IPricingStrategy randomStrategy = Random.value > 0.8f ? 
            new VIPPricingStrategy() : new StandardPricingStrategy();
        
        string story = _clientStories[Random.Range(0, _clientStories.Length)];
        if (randomStrategy is VIPPricingStrategy) story = "[VIP] " + story;

        Order newOrder = new Order(newCar, story, randomTime, randomStrategy);
    
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
        if (!_currentState.CanAcceptOrder()) return;

        _currentCarInBox = acceptedOrder.Car;
        _currentCarPayout = baseAcceptancePayout * _currentCarInBox.ClassMultiplier;
        _basePayoutSystem = new BasePayout(_currentCarPayout);
        _finalPayoutSystem = _basePayoutSystem;
        _currentState = new GarageOccupiedState();
        
        _activeOrders.Clear();
        UpdateOrderUI();
        UpdateUI();
        orderPanel?.SetActive(false);
        OpenRepairMenu();
        ResetServicesUI();
    }

    private void ResetServicesUI()
    {
        if (carWashRow != null)
        {
            carWashRow.infoText.text = "АВТОМИЙКА";
            carWashRow.priceText.text = $"ПОМИТИ ({carWashCost}$)";
            carWashRow.serviceButton.interactable = true;
            if (carWashRow.progressBarImage != null) carWashRow.progressBarImage.fillAmount = 0f;
        }

        if (interiorCleaningRow != null)
        {
            interiorCleaningRow.infoText.text = "ХІМЧИСТКА";
            interiorCleaningRow.priceText.text = $"ОЧИСТИТИ ({interiorCleaningCost}$)";
            interiorCleaningRow.serviceButton.interactable = true;
            if (interiorCleaningRow.progressBarImage != null) interiorCleaningRow.progressBarImage.fillAmount = 0f;
        }
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
    
    public void OpenRepairTab()
    {
        if (repairPage != null) repairPage.SetActive(true);
        if (servicePage != null) servicePage.SetActive(false);
    }

    public void OpenServicesTab()
    {
        if (repairPage != null) repairPage.SetActive(false);
        if (servicePage != null) servicePage.SetActive(true);
    }

    public void OpenRepairMenu()
    {
        if (_currentCarInBox == null) return;
        UpdateUI();
        repairMenuPanel.SetActive(true);
        OpenRepairTab();
    }

    public void CloseRepairMenu() => repairMenuPanel.SetActive(false);

    private void UpdateUI()
    {
        moneyText.text = $"Баланс: {PlayerMoney}$";
        
        if (levelText != null) 
            levelText.text = $"Рівень: {PlayerLevel}";
            
        if (xpProgressBar != null) 
            xpProgressBar.fillAmount = PlayerXP / GetXPForNextLevel();

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
        
        int reqPlayerLevel = 1;
        int reqToolLevel = 0;
        if (part == Vehicle.CarPart.RunningGear) { reqPlayerLevel = 2; reqToolLevel = 1; }
        else if (part == Vehicle.CarPart.Body) { reqPlayerLevel = 3; reqToolLevel = 2; }
        else if (part == Vehicle.CarPart.Electronics) { reqPlayerLevel = 4; reqToolLevel = 3; }

        bool isUnlocked = (PlayerLevel >= reqPlayerLevel && _toolUpgradeLevel >= reqToolLevel);
        
        if (cost <= 0)
        {
            row.priceText.text = "(ГОТОВО)";
            row.repairButton.interactable = false;
        }
        else if (!isUnlocked)
        {
            row.priceText.text = "НЕДОСТУПНО"; 
            row.repairButton.interactable = (_currentState != null && _currentState.CanPerformAction());
        }
        else
        {
            row.priceText.text = $"РЕМОНТ ({cost}$)";
            row.repairButton.interactable = (_currentState != null && _currentState.CanPerformAction());
        }

        if (row.progressBarImage != null && cost > 0)
            row.progressBarImage.fillAmount = 0f;
    }

    public void ProcessCarPart(Vehicle.CarPart part, RepairRowUI row)
    {
        if (_currentCarInBox == null || !_currentState.CanPerformAction()) return;
        
        int reqPlayerLevel = 1;
        int reqToolLevel = 0;
        string toolName = "Базовий набір";

        if (part == Vehicle.CarPart.RunningGear) { reqPlayerLevel = 2; reqToolLevel = 1; toolName = "Підйомник (Інструменти Рівень 1)"; }
        else if (part == Vehicle.CarPart.Body) { reqPlayerLevel = 3; reqToolLevel = 2; toolName = "Зварка (Інструменти Рівень 2)"; }
        else if (part == Vehicle.CarPart.Electronics) { reqPlayerLevel = 4; reqToolLevel = 3; toolName = "Сканер (Інструменти Рівень 3)"; }

        if (PlayerLevel < reqPlayerLevel || _toolUpgradeLevel < reqToolLevel)
        {
            ShowWarning($"ДЛЯ РЕМОНТУ ПОТРІБНО:\nРівень гравця: {reqPlayerLevel}\nНеобхідний інструмент:\n{toolName}");
            return;
        }

        float baseCost = GetRepairCost(part); 
        float costToPay = baseCost * (1.0f - (_discountUpgradeLevel * 0.1f));
        
        if (PlayerMoney >= costToPay) 
        {
            float totalTime = Mathf.Max(minRepairSpeed, baseRepairTime - (_toolUpgradeLevel * 0.5f));
            StartCoroutine(WorkRoutine(totalTime, row.priceText, "ЛАГОДЖУ...", row.progressBarImage, () => 
            {
                PlayerMoney -= costToPay; 
                _currentCarInBox.SetPartConditionToMax(part); 
                _basePayoutSystem.AddMoney(baseCost * profitMultiplier); 
                UpdateUI();
            }));
        }
        else
        {
            ShowWarning("НЕ ВИСТАЧАЄ ГРОШЕЙ НА ЗАПЧАСТИНИ!");
        }
    }

    public void AddCarWashService()
    {
        if (!_currentState.CanPerformAction()) return;
        if (PlayerMoney >= carWashCost)
        {
            StartCoroutine(WorkRoutine(serviceTime, carWashRow.priceText, "МИЮ...", carWashRow.progressBarImage, () => 
            {
                PlayerMoney -= carWashCost;
                _finalPayoutSystem = new CarWashDecorator(_finalPayoutSystem);
                carWashRow.serviceButton.interactable = false;
                carWashRow.priceText.text = "ВИКОНАНО";
                UpdateUI();
            }));
        }
    }

    public void AddInteriorCleaningService()
    {
        if (!_currentState.CanPerformAction()) return;
        if (PlayerMoney >= interiorCleaningCost)
        {
            StartCoroutine(WorkRoutine(serviceTime, interiorCleaningRow.priceText, "ЧИЩУ...", interiorCleaningRow.progressBarImage, () => 
            {
                PlayerMoney -= interiorCleaningCost;
                _finalPayoutSystem = new InteriorCleaningDecorator(_finalPayoutSystem);
                interiorCleaningRow.serviceButton.interactable = false;
                interiorCleaningRow.priceText.text = "ВИКОНАНО";
                UpdateUI();
            }));
        }
    }
    
    public void ShowWarning(string message)
    {
        if (warningText != null) warningText.text = message;
        if (warningPanel != null) warningPanel.SetActive(true);
    }
    
    public void CloseWarning()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }
    
    private System.Collections.IEnumerator WorkRoutine(float duration, TextMeshProUGUI statusText, string statusMessage, Image progressBar, System.Action onComplete)
    {
        _currentState = new GarageWorkingState();
        float elapsedTime = 0f;

        if (statusText != null) statusText.text = statusMessage;
        
        DisableAllButtons();

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            if (progressBar != null) progressBar.fillAmount = elapsedTime / duration;
            yield return null; 
        }
        
        if (progressBar != null) progressBar.fillAmount = 1f;
            
        _currentState = new GarageOccupiedState();;
        
        onComplete?.Invoke();
        
        if (carWashRow != null && carWashRow.priceText.text != "ВИКОНАНО")
            carWashRow.serviceButton.interactable = true;
        
        if (interiorCleaningRow != null && interiorCleaningRow.priceText.text != "ВИКОНАНО")
            interiorCleaningRow.serviceButton.interactable = true;
        
        SaveGame();
    }

    private void DisableAllButtons()
    {
        engineRow.repairButton.interactable = false;
        gearRow.repairButton.interactable = false;
        bodyRow.repairButton.interactable = false;
        electroRow.repairButton.interactable = false;
        if (carWashRow != null) carWashRow.serviceButton.interactable = false;
        if (interiorCleaningRow != null) interiorCleaningRow.serviceButton.interactable = false;
    }
    
    public void OnRepairEngineBtnClick() => ProcessCarPart(Vehicle.CarPart.Engine, engineRow);
    public void OnRepairGearBtnClick() => ProcessCarPart(Vehicle.CarPart.RunningGear, gearRow);
    public void OnRepairBodyBtnClick() => ProcessCarPart(Vehicle.CarPart.Body, bodyRow);
    public void OnRepairElectroBtnClick() => ProcessCarPart(Vehicle.CarPart.Electronics, electroRow);

    public void FinishWorkAndReturnCar()
    {
        if (!_currentState.CanPerformAction()) return; 

        float finalMoney = _finalPayoutSystem.CalculateFinalPayout(); 

        PlayerMoney += finalMoney; 

        AddXP(finalMoney); 

        _currentCarInBox = null;
        _currentCarPayout = 0f;
        _currentState = new GarageIdleState(); 

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
        if (!isPanelActive) UpdateShopUI();
        shopPanel.SetActive(!isPanelActive);
    }

    private void UpdateShopUI()
    {
        float nextToolPrice = 1000f + (_toolUpgradeLevel * 1000f);
        int reqPlayerLevelTool = _toolUpgradeLevel + 2; 

        toolUpgradeRow.infoText.text = $"ШВИДКІ ІНСТРУМЕНТИ (Рівень {_toolUpgradeLevel})";
        
        if (PlayerLevel < reqPlayerLevelTool)
        {
            toolUpgradeRow.priceText.text = $"НЕДОСТУПНО (з {reqPlayerLevelTool} Рівня)";
        }
        else
        {
            toolUpgradeRow.priceText.text = $"КУПИТИ ({nextToolPrice}$)";
        }
        
        toolUpgradeRow.buyButton.interactable = true; 
        
        float nextDiscountPrice = 1500f + (_discountUpgradeLevel * 1500f);
        int reqPlayerLevelDiscount = _discountUpgradeLevel + 2;

        discountUpgradeRow.infoText.text = $"ОПТОВІ ЗАКУПІВЛІ (Рівень {_discountUpgradeLevel})";
        
        if (PlayerLevel < reqPlayerLevelDiscount)
        {
            discountUpgradeRow.priceText.text = $"НЕДОСТУПНО (з {reqPlayerLevelDiscount} Рівня)";
        }
        else
        {
            discountUpgradeRow.priceText.text = $"КУПИТИ ({nextDiscountPrice}$)";
        }
        discountUpgradeRow.buyButton.interactable = true;
    }

    public void BuyToolUpgrade()
    {
        int requiredPlayerLevel = _toolUpgradeLevel + 2; 

        if (PlayerLevel < requiredPlayerLevel)
        {
            ShowWarning($"Цей інструмент доступний\nтільки з {requiredPlayerLevel} Рівня Гравця!");
            return;
        }

        float upgradePrice = 1000f + (_toolUpgradeLevel * 1000f); 
        if (PlayerMoney >= upgradePrice)
        {
            PlayerMoney -= upgradePrice;
            _toolUpgradeLevel++;
            SaveGame();
            UpdateUI();
            UpdateShopUI();
        }
    }

    public void BuyDiscountUpgrade()
    {
        int requiredPlayerLevel = _toolUpgradeLevel + 2; 

        if (PlayerLevel < requiredPlayerLevel)
        {
            ShowWarning($"Цей інструмент доступний\nтільки з {requiredPlayerLevel} Рівня Гравця!");
            return;
        }

        float upgradePrice = 1500f + (_toolUpgradeLevel * 1500f); 
        if (PlayerMoney >= upgradePrice)
        {
            PlayerMoney -= upgradePrice;
            _toolUpgradeLevel++;
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
        dataToSave.playerLevel = PlayerLevel;
        dataToSave.playerXP = PlayerXP;
        SaveManager.Save(dataToSave);
    }
}