using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CaisseToast.Wpf.Models;
using CaisseToast.Wpf.Services;

namespace CaisseToast.Wpf.ViewModels;

public partial class KitchenTicketViewModel : ObservableObject
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = "";
    public string TabName { get; init; } = "";
    public string Source { get; init; } = "";
    public string TimeLabel { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public KitchenTicketStatus Status { get; init; }
    public ObservableCollection<string> Lines { get; } = [];
}

public partial class KitchenViewModel : ObservableObject
{
    private readonly IPosStateService _pos;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string _newCount = "0";
    [ObservableProperty] private string _preparingCount = "0";
    [ObservableProperty] private string _readyCount = "0";

    public ObservableCollection<KitchenTicketViewModel> NewTickets { get; } = [];
    public ObservableCollection<KitchenTicketViewModel> PreparingTickets { get; } = [];
    public ObservableCollection<KitchenTicketViewModel> ReadyTickets { get; } = [];

    public KitchenViewModel(IPosStateService pos, INavigationService navigation)
    {
        _pos = pos;
        _navigation = navigation;
        _pos.StateChanged += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        NewTickets.Clear();
        PreparingTickets.Clear();
        ReadyTickets.Clear();

        foreach (var t in _pos.KitchenTickets.Where(t => t.Status != KitchenTicketStatus.Done))
        {
            var vm = ToVm(t);
            switch (t.Status)
            {
                case KitchenTicketStatus.New: NewTickets.Add(vm); break;
                case KitchenTicketStatus.Preparing: PreparingTickets.Add(vm); break;
                case KitchenTicketStatus.Ready: ReadyTickets.Add(vm); break;
            }
        }

        NewCount = NewTickets.Count.ToString();
        PreparingCount = PreparingTickets.Count.ToString();
        ReadyCount = ReadyTickets.Count.ToString();
    }

    [RelayCommand]
    private void AdvanceTicket(KitchenTicketViewModel? ticket)
    {
        if (ticket is null) return;
        _pos.AdvanceKitchenTicket(ticket.Id);
        Refresh();
    }

    [RelayCommand]
    private void BumpTicket(KitchenTicketViewModel? ticket)
    {
        if (ticket is null) return;
        _pos.BumpKitchenTicket(ticket.Id);
        Refresh();
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateTo(AppScreen.Home);

    private static KitchenTicketViewModel ToVm(KitchenTicket t)
    {
        var vm = new KitchenTicketViewModel
        {
            Id = t.Id,
            OrderNumber = t.OrderNumber,
            TabName = t.TabName,
            Source = t.Source,
            TimeLabel = t.TimeLabel,
            Status = t.Status,
            StatusLabel = t.Status switch
            {
                KitchenTicketStatus.New => "Nouveau",
                KitchenTicketStatus.Preparing => "En cours",
                KitchenTicketStatus.Ready => "Prêt",
                _ => "Terminé"
            }
        };
        foreach (var line in t.Lines)
            vm.Lines.Add($"{line.Quantity}× {line.Name}");
        return vm;
    }
}
