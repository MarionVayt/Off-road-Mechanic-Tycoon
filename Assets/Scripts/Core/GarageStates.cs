public interface IGarageState
{
    bool CanAcceptOrder();
    bool CanPerformAction();
    string GetStatusName();
}

public class GarageIdleState : IGarageState
{
    public bool CanAcceptOrder() => true;
    public bool CanPerformAction() => false;
    public string GetStatusName() => "ОЧІКУВАННЯ КЛІЄНТІВ";
}

public class GarageOccupiedState : IGarageState
{
    public bool CanAcceptOrder() => false;
    public bool CanPerformAction() => true;
    public string GetStatusName() => "АВТО В БОКСІ (ОЧІКУЄ ДІЙ)";
}

public class GarageWorkingState : IGarageState
{
    public bool CanAcceptOrder() => false;
    public bool CanPerformAction() => false;
    public string GetStatusName() => "ЙДЕ РОБОТА...";
}