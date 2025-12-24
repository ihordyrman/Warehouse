namespace Warehouse.App.Old.Pages.Models;

public class AccountViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public bool HasCredentials { get; set; }

    public BalanceViewModel? Balance { get; set; }
}

public class BalanceViewModel
{
    public decimal Available { get; set; }

    public decimal InOrders { get; set; }

    public decimal Total { get; set; }
}

public class PipelineViewModel
{
    public int Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string MarketType { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string? Strategy { get; set; }

    public string? Interval { get; set; }

    public string Status { get; set; } = "Idle";

    public DateTime? LastRun { get; set; }
}

public class DashboardViewModel
{
    public List<AccountViewModel> Accounts { get; set; } = new();

    public List<PipelineViewModel> Pipelines { get; set; } = new();

    public int ActiveAccountsCount { get; set; }

    public int RunningPipelinesCount { get; set; }

    public decimal TotalBalance { get; set; }
}
