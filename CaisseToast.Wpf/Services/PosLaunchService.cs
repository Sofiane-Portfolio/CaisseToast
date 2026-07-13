namespace CaisseToast.Wpf.Services;

public interface IPosLaunchService
{
    int? TableNumber { get; set; }
    int? ResumeOrderId { get; set; }
    string? TabName { get; set; }
    void Clear();
}

public sealed class PosLaunchService : IPosLaunchService
{
    public int? TableNumber { get; set; }
    public int? ResumeOrderId { get; set; }
    public string? TabName { get; set; }

    public void Clear()
    {
        TableNumber = null;
        ResumeOrderId = null;
        TabName = null;
    }
}
