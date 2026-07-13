namespace CaisseToast.Wpf.Models;

public sealed class TablePosition
{
    public int Number { get; init; }
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class PosSnapshot
{
    public Dictionary<int, TableRuntimeState> TableStates { get; set; } = new();
    public Dictionary<int, TablePosition> TablePositions { get; set; } = new();
    public bool TablePositionsCustom { get; set; }
    public List<PosOrder> OpenOrders { get; set; } = [];
    public List<PosOrder> PaidOrders { get; set; } = [];
    public List<PosOrder> ClosedOrders { get; set; } = [];
    public List<KitchenTicket> KitchenTickets { get; set; } = [];
    public List<OnlineOrder> OnlineOrders { get; set; } = [];
    public int OrderSequence { get; set; } = 19;
    public int TicketSequence { get; set; } = 1;
    public int PaidToday { get; set; }
    public decimal RevenueToday { get; set; }
    public bool IsClockedIn { get; set; }
    public DateTime? ShiftStart { get; set; }
}
