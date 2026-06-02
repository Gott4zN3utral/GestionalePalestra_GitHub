namespace GestionalePalestra.Models;

public sealed class DashboardStats
{
    public int TotalMembers { get; set; }
    public int ActiveMemberships { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TodayAttendances { get; set; }
}
