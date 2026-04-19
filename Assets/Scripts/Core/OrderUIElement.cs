using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OrderUIElement : MonoBehaviour
{
    [Header("UI Лінки")]
    public TextMeshProUGUI carNameText;
    public TextMeshProUGUI storyText;
    public Slider timeSlider;
    public Button acceptButton;

    private Order _myOrder;
    private GarageManager _manager;

    public void Setup(Order order, GarageManager manager)
    {
        _myOrder = order;
        _manager = manager;
        
        carNameText.text = $"{order.Car.Brand} {order.Car.Model}";
        storyText.text = order.ClientStory;
        
        timeSlider.maxValue = order.MaxTime;
        timeSlider.value = order.TimeLeft;
        
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(OnAcceptClicked);
    }

    private void Update()
    {
        if (_myOrder != null)
        {
            timeSlider.value = _myOrder.TimeLeft;
        }
    }

    private void OnAcceptClicked()
    {
        _manager.AcceptOrder(_myOrder);
    }
}