using CaisseToast.Wpf.Models;

namespace CaisseToast.Wpf.Services;

public interface IPosStateService
{
    IReadOnlyList<TableDefinition> Tables { get; }
    IReadOnlyList<Product> Products { get; }
    IReadOnlyList<PosOrder> OpenOrders { get; }
    IReadOnlyList<PosOrder> PaidOrders { get; }
    IReadOnlyList<PosOrder> ClosedOrders { get; }
    IReadOnlyList<KitchenTicket> KitchenTickets { get; }
    IReadOnlyList<OnlineOrder> OnlineOrders { get; }

    bool IsClockedIn { get; }
    DateTime? ShiftStart { get; }
    bool TablePositionsCustom { get; }
    int OpenOrderCount { get; }
    int PaidTodayCount { get; }
    decimal RevenueToday { get; }
    int NextTicketNumber { get; }

    TableRuntimeState GetTableState(int number);
    TablePosition GetTablePosition(int number);
    void SetTableStatus(int number, TableStatus status);
    void CycleTableStatus(int number);
    void SetTablePosition(int number, double x, double y);
    void ResetTablePositions();
    void InitDefaultGridPositions();
    (int free, int occupied, int bill, int paying, int reserved, int dirty, int unavailable) GetTableCounts();

    void ClockIn();
    void ClockOut();
    string FormatShiftDuration();

    PosOrder CreateOrder(int? tableNumber, string tabName, string source, string serverName);
    PosOrder? GetOrder(int id);
    PosOrder? FindOpenOrderForTable(int tableNumber);
    void SaveOrder(PosOrder order);
    void HoldOrder(PosOrder order);
    PosOrder? ResumeOrder(int id);
    void SendToKitchen(PosOrder order);
    void RequestBill(int tableNumber);
    void PayOrder(int orderId, string cashier, string method);
    void CloseOrder(int orderId);
    void RefundOrder(int orderId);
    void CancelOpenOrder(int orderId);

    void AdvanceKitchenTicket(int ticketId);
    void BumpKitchenTicket(int ticketId);

    HeldOrder SubmitKioskOrder(IEnumerable<KioskCartLine> lines, KioskDiningMode dining);
    void AcceptOnlineOrder(int id);
    void MarkOnlineReady(int id);
    void RejectOnlineOrder(int id);
    void SeedOnlineDemo();

    IReadOnlyList<PosOrder> GetOpenOrdersForServer(string serverName);
    ServerReportData GetServerReport(string serverName);

    event Action? StateChanged;
}

public sealed class PosStateService : IPosStateService
{
    private readonly IPosStorageService _storage;
    private readonly Dictionary<int, TableRuntimeState> _tableStates = new();
    private readonly Dictionary<int, TablePosition> _tablePositions = new();
    private readonly List<PosOrder> _openOrders = [];
    private readonly List<PosOrder> _paidOrders = [];
    private readonly List<PosOrder> _closedOrders = [];
    private readonly List<KitchenTicket> _kitchenTickets = [];
    private readonly List<OnlineOrder> _onlineOrders = [];
    private int _orderSequence = 19;
    private int _ticketSequence = 1;
    private int _paidToday = 16;
    private decimal _revenue = 252.21m;

    public IReadOnlyList<TableDefinition> Tables { get; }
    public IReadOnlyList<Product> Products { get; }
    public IReadOnlyList<PosOrder> OpenOrders => _openOrders;
    public IReadOnlyList<PosOrder> PaidOrders => _paidOrders;
    public IReadOnlyList<PosOrder> ClosedOrders => _closedOrders;
    public IReadOnlyList<KitchenTicket> KitchenTickets => _kitchenTickets;
    public IReadOnlyList<OnlineOrder> OnlineOrders => _onlineOrders;

    public bool IsClockedIn { get; private set; }
    public DateTime? ShiftStart { get; private set; }
    public bool TablePositionsCustom { get; private set; }
    public int OpenOrderCount => _openOrders.Count;
    public int PaidTodayCount => _paidToday;
    public decimal RevenueToday => _revenue;
    public int NextTicketNumber => _orderSequence;

    public event Action? StateChanged;

    public PosStateService(IPosStorageService storage)
    {
        _storage = storage;
        Tables = BuildTables();
        Products = BuildProducts();

        if (_storage.TryLoad(out var snapshot))
            ApplySnapshot(snapshot);
        else
            SeedDemoData();
    }

    public TableRuntimeState GetTableState(int number)
    {
        if (!_tableStates.TryGetValue(number, out var state))
        {
            state = new TableRuntimeState();
            _tableStates[number] = state;
        }
        return state;
    }

    public TablePosition GetTablePosition(int number)
    {
        if (!_tablePositions.TryGetValue(number, out var pos))
        {
            pos = new TablePosition { Number = number };
            _tablePositions[number] = pos;
        }
        return pos;
    }

    public void SetTableStatus(int number, TableStatus status)
    {
        var state = GetTableState(number);
        state.Status = status;
        if (status is TableStatus.Free or TableStatus.Dirty or TableStatus.Unavailable or TableStatus.Reserved)
        {
            state.GuestName = null;
            state.OrderTotal = 0;
            state.ActiveOrderId = null;
        }
        Notify();
    }

    public void CycleTableStatus(int number)
    {
        var state = GetTableState(number);
        if (state.Status is TableStatus.Occupied or TableStatus.Bill or TableStatus.Paying)
            return;

        var cycle = new[] { TableStatus.Free, TableStatus.Reserved, TableStatus.Dirty, TableStatus.Unavailable };
        var idx = Array.IndexOf(cycle, state.Status);
        SetTableStatus(number, cycle[(idx + 1) % cycle.Length]);
    }

    public void SetTablePosition(int number, double x, double y)
    {
        var pos = GetTablePosition(number);
        pos.X = Math.Max(0, x);
        pos.Y = Math.Max(0, y);
        TablePositionsCustom = true;
        Notify();
    }

    public void ResetTablePositions()
    {
        _tablePositions.Clear();
        TablePositionsCustom = false;
        InitDefaultGridPositions();
        Notify();
    }

    public void InitDefaultGridPositions()
    {
        const int cols = 10;
        const double w = 104;
        const double h = 104;
        var col = 0;
        var row = 0;

        foreach (var t in Tables.Where(t => t.Zone == "salle"))
        {
            var pos = GetTablePosition(t.Number);
            pos.X = col * w + 12;
            pos.Y = row * h + 40;
            col++;
            if (col >= cols) { col = 0; row++; }
        }
    }

    public (int free, int occupied, int bill, int paying, int reserved, int dirty, int unavailable) GetTableCounts()
    {
        var counts = (free: 0, occupied: 0, bill: 0, paying: 0, reserved: 0, dirty: 0, unavailable: 0);
        foreach (var t in Tables)
        {
            switch (GetTableState(t.Number).Status)
            {
                case TableStatus.Free: counts.free++; break;
                case TableStatus.Occupied: counts.occupied++; break;
                case TableStatus.Bill: counts.bill++; break;
                case TableStatus.Paying: counts.paying++; break;
                case TableStatus.Reserved: counts.reserved++; break;
                case TableStatus.Dirty: counts.dirty++; break;
                case TableStatus.Unavailable: counts.unavailable++; break;
            }
        }
        return counts;
    }

    public void ClockIn()
    {
        IsClockedIn = true;
        ShiftStart = DateTime.Now;
        Notify();
    }

    public void ClockOut()
    {
        IsClockedIn = false;
        ShiftStart = null;
        Notify();
    }

    public string FormatShiftDuration()
    {
        if (!IsClockedIn || ShiftStart is null) return "0:00";
        var span = DateTime.Now - ShiftStart.Value;
        return span.Hours > 0 ? $"{span.Hours}:{span.Minutes:D2}" : $"0:{span.Minutes:D2}";
    }

    public PosOrder CreateOrder(int? tableNumber, string tabName, string source, string serverName)
    {
        var order = new PosOrder
        {
            Id = Environment.TickCount + _openOrders.Count,
            Number = $"#{_orderSequence:D4}",
            TableNumber = tableNumber,
            TabName = tabName,
            Source = source,
            ServerName = serverName,
            Status = OrderStatus.Open
        };
        _orderSequence++;
        return order;
    }

    public PosOrder? GetOrder(int id) =>
        _openOrders.FirstOrDefault(o => o.Id == id)
        ?? _paidOrders.FirstOrDefault(o => o.Id == id)
        ?? _closedOrders.FirstOrDefault(o => o.Id == id);

    public PosOrder? FindOpenOrderForTable(int tableNumber) =>
        _openOrders.FirstOrDefault(o => o.TableNumber == tableNumber);

    public void SaveOrder(PosOrder order)
    {
        var existing = _openOrders.FirstOrDefault(o => o.Id == order.Id);
        if (existing is null)
            _openOrders.Insert(0, order);
        else
        {
            var idx = _openOrders.IndexOf(existing);
            _openOrders[idx] = order;
        }

        if (order.TableNumber is int table)
        {
            var st = GetTableState(table);
            st.ActiveOrderId = order.Id;
            st.GuestName = string.IsNullOrWhiteSpace(order.TabName) ? null : order.TabName;
            st.OrderTotal = order.Total;
            if (st.Status is TableStatus.Free or TableStatus.Reserved)
                st.Status = TableStatus.Occupied;
        }
        Notify();
    }

    public void HoldOrder(PosOrder order)
    {
        if (!_openOrders.Any(o => o.Id == order.Id))
            _openOrders.Insert(0, order);
        SaveOrder(order);
    }

    public PosOrder? ResumeOrder(int id) => _openOrders.FirstOrDefault(o => o.Id == id);

    public void SendToKitchen(PosOrder order)
    {
        var newLines = order.Lines.Where(l => !l.SentToKitchen).ToList();
        if (newLines.Count == 0) return;

        foreach (var line in newLines)
            line.SentToKitchen = true;

        _kitchenTickets.Insert(0, new KitchenTicket
        {
            Id = _ticketSequence++,
            OrderNumber = order.Number,
            Source = order.Source,
            TabName = order.DisplayName,
            Lines = newLines.Select(l => new OrderLine
            {
                ProductId = l.ProductId,
                Name = l.DisplayName,
                Price = l.Price,
                Quantity = l.Quantity,
                SentToKitchen = true,
                Modifiers = [..l.Modifiers]
            }).ToList(),
            Status = KitchenTicketStatus.New
        });

        HoldOrder(order);
    }

    public void RequestBill(int tableNumber) => SetTableStatus(tableNumber, TableStatus.Bill);

    public void PayOrder(int orderId, string cashier, string method)
    {
        var idx = _openOrders.FindIndex(o => o.Id == orderId);
        if (idx < 0) return;

        var order = _openOrders[idx];
        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.Now;
        order.CashierName = cashier;
        order.PaymentMethod = method;
        _openOrders.RemoveAt(idx);
        _paidOrders.Insert(0, order);
        _paidToday++;
        _revenue += order.Total;

        if (order.TableNumber is int table)
        {
            SetTableStatus(table, TableStatus.Paying);
            GetTableState(table).OrderTotal = order.Total;
        }
        else Notify();
    }

    public void CloseOrder(int orderId)
    {
        var idx = _paidOrders.FindIndex(o => o.Id == orderId);
        if (idx < 0) return;

        var order = _paidOrders[idx];
        order.Status = OrderStatus.Closed;
        order.ClosedAt = DateTime.Now;
        _paidOrders.RemoveAt(idx);
        _closedOrders.Insert(0, order);

        if (order.TableNumber is int table)
            SetTableStatus(table, TableStatus.Dirty);
        else Notify();
    }

    public void RefundOrder(int orderId)
    {
        var idx = _paidOrders.FindIndex(o => o.Id == orderId);
        if (idx < 0) return;

        var order = _paidOrders[idx];
        _paidOrders.RemoveAt(idx);
        _paidToday = Math.Max(0, _paidToday - 1);
        _revenue = Math.Max(0, _revenue - order.Total);
        Notify();
    }

    public void CancelOpenOrder(int orderId)
    {
        var order = _openOrders.FirstOrDefault(o => o.Id == orderId);
        if (order is null) return;

        _openOrders.Remove(order);
        if (order.TableNumber is int table && GetTableState(table).ActiveOrderId == orderId)
            SetTableStatus(table, TableStatus.Free);
        else Notify();
    }

    public void AdvanceKitchenTicket(int ticketId)
    {
        var ticket = _kitchenTickets.FirstOrDefault(t => t.Id == ticketId);
        if (ticket is null) return;

        ticket.Status = ticket.Status switch
        {
            KitchenTicketStatus.New => KitchenTicketStatus.Preparing,
            KitchenTicketStatus.Preparing => KitchenTicketStatus.Ready,
            KitchenTicketStatus.Ready => KitchenTicketStatus.Done,
            _ => KitchenTicketStatus.Done
        };
        Notify();
    }

    public void BumpKitchenTicket(int ticketId)
    {
        var ticket = _kitchenTickets.FirstOrDefault(t => t.Id == ticketId);
        if (ticket is null) return;
        ticket.Status = KitchenTicketStatus.Done;
        Notify();
    }

    public HeldOrder SubmitKioskOrder(IEnumerable<KioskCartLine> lines, KioskDiningMode dining)
    {
        var list = lines.ToList();
        var orderLines = list.Select(l => new OrderLine
        {
            ProductId = l.ProductId,
            Name = l.Name,
            Price = l.Price,
            Quantity = l.Quantity,
            SentToKitchen = true
        }).ToList();

        var diningLbl = dining == KioskDiningMode.DineIn ? "Sur place" : "À emporter";
        var order = CreateOrder(null, $"Kiosk · {diningLbl}", "Kiosk", "Kiosk");
        order.Lines = orderLines;
        SendToKitchen(order);

        return new HeldOrder
        {
            Id = order.Id,
            Number = order.Number,
            Total = order.Total,
            Source = "Kiosk",
            TabName = order.TabName
        };
    }

    public void AcceptOnlineOrder(int id)
    {
        var order = _onlineOrders.FirstOrDefault(o => o.Id == id);
        if (order is null || order.Status != "pending") return;
        order.Status = "accepted";

        var posOrder = CreateOrder(null, order.Customer, "Online", "Online");
        posOrder.Lines = order.Items.Count > 0
            ? order.Items.Select(CloneLine).ToList()
            : [new OrderLine { ProductId = 0, Name = $"{order.Channel} · {order.Customer}", Price = order.Total, Quantity = 1, SentToKitchen = true }];
        SendToKitchen(posOrder);
    }

    public void MarkOnlineReady(int id)
    {
        var order = _onlineOrders.FirstOrDefault(o => o.Id == id);
        if (order is null || order.Status != "accepted") return;
        order.Status = "ready";
        Notify();
    }

    public void RejectOnlineOrder(int id)
    {
        var order = _onlineOrders.FirstOrDefault(o => o.Id == id);
        if (order is null) return;
        _onlineOrders.Remove(order);
        Notify();
    }

    public void SeedOnlineDemo()
    {
        var samples = new[]
        {
            ("Marie L.", "Uber Eats", new[] { ("Burger Gourmet", 12.90m), ("Coca Cola", 2.90m) }),
            ("Karim B.", "Web", new[] { ("Pizza Regina", 13.50m), ("Tiramisu", 6.50m) }),
            ("Emma R.", "Deliveroo", new[] { ("Salade César", 9.90m) }),
            ("Lucas P.", "Takeout", new[] { ("Sandwich Club", 8.90m), ("Jus d'Orange", 3.50m) })
        };
        var pick = samples[Random.Shared.Next(samples.Length)];
        var items = pick.Item3.Select(i => new OrderLine { Name = i.Item1, Price = i.Item2, Quantity = 1 }).ToList();
        var subtotal = items.Sum(i => i.LineTotal);
        var total = Math.Round(subtotal * 1.10m, 2);

        _onlineOrders.Insert(0, new OnlineOrder
        {
            Id = Environment.TickCount,
            Customer = pick.Item1,
            Channel = pick.Item2,
            Total = total,
            ItemCount = items.Count,
            Status = "pending",
            Time = DateTime.Now.ToString("HH:mm"),
            Items = items
        });
        Notify();
    }

    public IReadOnlyList<PosOrder> GetOpenOrdersForServer(string serverName) =>
        _openOrders.Where(o => o.ServerName == serverName).ToList();

    public ServerReportData GetServerReport(string serverName)
    {
        var serverOrders = _paidOrders.Where(o => o.ServerName == serverName).ToList();
        var openForServer = _openOrders.Where(o => o.ServerName == serverName).ToList();
        return new ServerReportData
        {
            OrdersCount = serverOrders.Count + openForServer.Count,
            Revenue = serverOrders.Sum(o => o.Total),
            Tips = serverOrders.Sum(o => o.TipAmount),
            TablesServed = openForServer.Count(o => o.TableNumber.HasValue) + serverOrders.Count(o => o.TableNumber.HasValue),
            ShiftDuration = FormatShiftDuration()
        };
    }

    private void Notify()
    {
        _storage.Save(BuildSnapshot());
        StateChanged?.Invoke();
    }

    private PosSnapshot BuildSnapshot() => new()
    {
        TableStates = _tableStates.ToDictionary(kv => kv.Key, kv => kv.Value),
        TablePositions = _tablePositions.ToDictionary(kv => kv.Key, kv => kv.Value),
        TablePositionsCustom = TablePositionsCustom,
        OpenOrders = [.._openOrders],
        PaidOrders = [.._paidOrders],
        ClosedOrders = [.._closedOrders],
        KitchenTickets = [.._kitchenTickets],
        OnlineOrders = [.._onlineOrders],
        OrderSequence = _orderSequence,
        TicketSequence = _ticketSequence,
        PaidToday = _paidToday,
        RevenueToday = _revenue,
        IsClockedIn = IsClockedIn,
        ShiftStart = ShiftStart
    };

    private void ApplySnapshot(PosSnapshot snapshot)
    {
        _tableStates.Clear();
        foreach (var kv in snapshot.TableStates)
            _tableStates[kv.Key] = kv.Value;

        _tablePositions.Clear();
        foreach (var kv in snapshot.TablePositions)
            _tablePositions[kv.Key] = kv.Value;

        TablePositionsCustom = snapshot.TablePositionsCustom;
        _openOrders.Clear();
        _openOrders.AddRange(snapshot.OpenOrders);
        _paidOrders.Clear();
        _paidOrders.AddRange(snapshot.PaidOrders);
        _closedOrders.Clear();
        _closedOrders.AddRange(snapshot.ClosedOrders);
        _kitchenTickets.Clear();
        _kitchenTickets.AddRange(snapshot.KitchenTickets);
        _onlineOrders.Clear();
        _onlineOrders.AddRange(snapshot.OnlineOrders);

        _orderSequence = snapshot.OrderSequence;
        _ticketSequence = snapshot.TicketSequence;
        _paidToday = snapshot.PaidToday;
        _revenue = snapshot.RevenueToday;
        IsClockedIn = snapshot.IsClockedIn;
        ShiftStart = snapshot.ShiftStart;

        if (!TablePositionsCustom && _tablePositions.Count == 0)
            InitDefaultGridPositions();
    }

    private void SeedDemoData()
    {
        InitDefaultGridPositions();
        SeedDemoTables();
        SeedDemoOrders();
        SeedKitchen();
        SeedOnline();
        Notify();
    }

    private static OrderLine CloneLine(OrderLine l) => new()
    {
        ProductId = l.ProductId,
        Name = l.Name,
        Price = l.Price,
        Quantity = l.Quantity,
        SentToKitchen = l.SentToKitchen,
        Modifiers = [..l.Modifiers]
    };

    private static IReadOnlyList<TableDefinition> BuildTables()
    {
        var list = new List<TableDefinition>();
        for (var i = 1; i <= 20; i++)
            list.Add(new TableDefinition { Number = i, Seats = i % 3 == 0 ? 2 : 4, Zone = "salle" });
        for (var i = 21; i <= 30; i++)
            list.Add(new TableDefinition { Number = i, Seats = 4, Zone = "terrasse" });
        for (var i = 31; i <= 36; i++)
            list.Add(new TableDefinition { Number = i, Seats = 2, Zone = "bar", IsBar = true });
        for (var i = 37; i <= 42; i++)
            list.Add(new TableDefinition { Number = i, Seats = i % 2 == 1 ? 6 : 4, Zone = "vip", IsVip = true });
        return list;
    }

    private static IReadOnlyList<Product> BuildProducts() =>
    [
        new() { Id = 1, Name = "Burger Gourmet", Price = 12.90m, Stock = 24, Category = ProductCategory.Food,
            ImageUrl = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=800&h=600&fit=crop&q=80",
            ModifierOptions = ["Sans oignon", "Extra fromage", "Bien cuit"] },
        new() { Id = 2, Name = "Frites Croustill.", Price = 3.90m, Stock = 42, Category = ProductCategory.Food,
            ImageUrl = "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=800&h=600&fit=crop&q=80" },
        new() { Id = 3, Name = "Coca Cola", Price = 2.90m, Stock = 5, Category = ProductCategory.Drinks,
            ImageUrl = "https://images.unsplash.com/photo-1629203851122-3726ecdf080e?w=800&h=600&fit=crop&q=80" },
        new() { Id = 4, Name = "Pizza Regina", Price = 13.50m, Stock = 18, Category = ProductCategory.Food,
            ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=800&h=600&fit=crop&q=80",
            ModifierOptions = ["Sans olives", "Pâte fine", "Extra mozzarella"] },
        new() { Id = 5, Name = "Café Espresso", Price = 2.20m, Stock = 3, Category = ProductCategory.Drinks,
            ImageUrl = "https://images.unsplash.com/photo-1514434753130-a37f68e6c6e7?w=800&h=600&fit=crop&q=80",
            ModifierOptions = ["Sans sucre", "Décaféiné", "Extra shot"] },
        new() { Id = 6, Name = "Tiramisu", Price = 6.50m, Stock = 12, Category = ProductCategory.Desserts,
            ImageUrl = "https://images.unsplash.com/photo-1571877227200-a0d98ea607e9?w=800&h=600&fit=crop&q=80" },
        new() { Id = 7, Name = "Salade César", Price = 9.90m, Stock = 8, Category = ProductCategory.Food,
            ImageUrl = "https://images.unsplash.com/photo-1546793665-c74683f339c1?w=800&h=600&fit=crop&q=80" },
        new() { Id = 8, Name = "Muffin Choco", Price = 3.50m, Stock = 20, Category = ProductCategory.Desserts,
            ImageUrl = "https://images.unsplash.com/photo-1607958996333-41aef7caef51?w=800&h=600&fit=crop&q=80" },
        new() { Id = 9, Name = "Bière Artisanale", Price = 5.80m, Stock = 15, Category = ProductCategory.Drinks,
            ImageUrl = "https://images.unsplash.com/photo-1608270586650-53e8dd822f5c?w=800&h=600&fit=crop&q=80" },
        new() { Id = 10, Name = "Jus d'Orange", Price = 3.50m, Stock = 30, Category = ProductCategory.Drinks,
            ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0e?w=800&h=600&fit=crop&q=80" },
        new() { Id = 11, Name = "Sandwich Club", Price = 8.90m, Stock = 10, Category = ProductCategory.Food,
            ImageUrl = "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=800&h=600&fit=crop&q=80" },
        new() { Id = 12, Name = "Smoothie Fruits", Price = 4.90m, Stock = 8, Category = ProductCategory.Drinks,
            ImageUrl = "https://images.unsplash.com/photo-1505252587541-944859a8d252?w=800&h=600&fit=crop&q=80",
            ModifierOptions = ["Sans glace", "Extra fruits", "Sans lactose"] },
    ];

    private void SeedDemoTables()
    {
        SetTableStatus(5, TableStatus.Dirty);
        SetTableStatus(8, TableStatus.Unavailable);
        SetTableStatus(3, TableStatus.Occupied);
        GetTableState(3).GuestName = "Marc";
        GetTableState(3).OrderTotal = 20.24m;
        SetTableStatus(14, TableStatus.Occupied);
        GetTableState(14).OrderTotal = 35.50m;
        SetTableStatus(15, TableStatus.Reserved);
        SetTableStatus(18, TableStatus.Reserved);
    }

    private void SeedDemoOrders()
    {
        var o1 = CreateOrder(null, "Sarah L.", "POS", "Marie");
        o1.Lines =
        [
            new OrderLine { ProductId = 1, Name = "Burger Gourmet", Price = 12.90m, Quantity = 2 },
            new OrderLine { ProductId = 3, Name = "Coca Cola", Price = 2.90m, Quantity = 2 }
        ];
        _openOrders.Add(o1);

        var o2 = CreateOrder(null, "Quick", "POS", "Thomas");
        o2.Lines = [new OrderLine { ProductId = 4, Name = "Pizza Regina", Price = 13.50m, Quantity = 1 }];
        _openOrders.Add(o2);

        var o3 = CreateOrder(3, "Marc", "POS", "Thomas");
        o3.Lines =
        [
            new OrderLine { ProductId = 1, Name = "Burger Gourmet", Price = 12.90m, Quantity = 1 },
            new OrderLine { ProductId = 2, Name = "Frites Croustill.", Price = 3.90m, Quantity = 1, SentToKitchen = true }
        ];
        _openOrders.Add(o3);
        GetTableState(3).ActiveOrderId = o3.Id;
    }

    private void SeedKitchen()
    {
        _kitchenTickets.Add(new KitchenTicket
        {
            Id = _ticketSequence++,
            OrderNumber = "#0017",
            Source = "POS",
            TabName = "Sarah L.",
            Status = KitchenTicketStatus.Preparing,
            Lines =
            [
                new OrderLine { ProductId = 1, Name = "Burger Gourmet", Price = 12.90m, Quantity = 2 },
                new OrderLine { ProductId = 3, Name = "Coca Cola", Price = 2.90m, Quantity = 2 }
            ]
        });
    }

    private void SeedOnline()
    {
        _onlineOrders.Add(new OnlineOrder
        {
            Id = 1, Customer = "Marie L.", Channel = "Uber Eats", Total = 15.80m, ItemCount = 2,
            Status = "pending", Time = "12:34",
            Items =
            [
                new OrderLine { Name = "Burger Gourmet", Price = 12.90m, Quantity = 1 },
                new OrderLine { Name = "Coca Cola", Price = 2.90m, Quantity = 1 }
            ]
        });
        _onlineOrders.Add(new OnlineOrder
        {
            Id = 2, Customer = "Karim B.", Channel = "Web", Total = 20.00m, ItemCount = 2,
            Status = "pending", Time = "12:41",
            Items =
            [
                new OrderLine { Name = "Pizza Regina", Price = 13.50m, Quantity = 1 },
                new OrderLine { Name = "Tiramisu", Price = 6.50m, Quantity = 1 }
            ]
        });
    }
}
