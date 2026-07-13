namespace CaisseToast.Wpf.Models;

public enum TableStatus
{
    Free,
    Occupied,
    Bill,
    Paying,
    Reserved,
    Dirty,
    Unavailable
}

public enum ProductCategory
{
    Food,
    Drinks,
    Desserts
}

public enum KioskDiningMode
{
    DineIn,
    TakeOut
}

public enum OrderStatus
{
    Open,
    Paid,
    Closed
}

public enum KitchenTicketStatus
{
    New,
    Preparing,
    Ready,
    Done
}

public sealed class TableDefinition
{
    public int Number { get; init; }
    public int Seats { get; init; }
    public string Zone { get; init; } = "salle";
    public bool IsVip { get; init; }
    public bool IsBar { get; init; }
}

public sealed class TableRuntimeState
{
    public TableStatus Status { get; set; } = TableStatus.Free;
    public string? GuestName { get; set; }
    public decimal OrderTotal { get; set; }
    public int? ActiveOrderId { get; set; }
}

public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int Stock { get; set; }
    public ProductCategory Category { get; init; }
    public string ImageUrl { get; init; } = "";
    public string[] ModifierOptions { get; init; } = [];
    public bool HasModifiers => ModifierOptions.Length > 0;
    public string CategoryLabel => Category switch
    {
        ProductCategory.Food => "Plats",
        ProductCategory.Drinks => "Boissons",
        _ => "Desserts"
    };
}

public sealed class OrderLine
{
    public int ProductId { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool SentToKitchen { get; set; }
    public List<string> Modifiers { get; set; } = [];
    public string DisplayName => Modifiers.Count > 0 ? $"{Name} ({string.Join(" · ", Modifiers)})" : Name;
    public decimal LineTotal => Price * Quantity;
}

public sealed class PosOrder
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public int? TableNumber { get; set; }
    public string TabName { get; set; } = "";
    public string Source { get; set; } = "POS";
    public string ServerName { get; set; } = "";
    public List<OrderLine> Lines { get; set; } = [];
    public decimal TipAmount { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CashierName { get; set; }
    public string? PaymentMethod { get; set; }

    public string TimeLabel => CreatedAt.ToString("HH:mm");
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public decimal Tax => Math.Round(Subtotal * 0.10m, 2);
    public decimal Total => Subtotal + Tax + TipAmount;
    public int ItemCount => Lines.Sum(l => l.Quantity);
    public string DisplayName => !string.IsNullOrWhiteSpace(TabName)
        ? TabName
        : TableNumber is int n ? $"Table T{n}" : "Quick Order";
}

public sealed class KitchenTicket
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string Source { get; init; } = "";
    public string TabName { get; init; } = "";
    public List<OrderLine> Lines { get; set; } = [];
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public KitchenTicketStatus Status { get; set; } = KitchenTicketStatus.New;

    public string TimeLabel => CreatedAt.ToString("HH:mm");
    public int ItemCount => Lines.Sum(l => l.Quantity);
}

public sealed class OnlineOrder
{
    public int Id { get; init; }
    public string Customer { get; init; } = "";
    public string Channel { get; init; } = "";
    public decimal Total { get; init; }
    public int ItemCount { get; init; }
    public string Status { get; set; } = "pending";
    public string Time { get; init; } = "";
    public List<OrderLine> Items { get; set; } = [];
}

public sealed class ServerReportData
{
    public int OrdersCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal Tips { get; init; }
    public int TablesServed { get; init; }
    public string ShiftDuration { get; init; } = "";
}

public sealed class KioskCartLine
{
    public int ProductId { get; init; }
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public sealed class HeldOrder
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public decimal Total { get; init; }
    public string Source { get; init; } = "";
    public string TabName { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

public static class TableStatusLabels
{
    public static string Get(TableStatus status) => status switch
    {
        TableStatus.Occupied => "Occupée",
        TableStatus.Bill => "Addition",
        TableStatus.Paying => "En paiement",
        TableStatus.Reserved => "Réservée",
        TableStatus.Dirty => "Sale",
        TableStatus.Unavailable => "Non dispo.",
        _ => "Libre"
    };
}
