namespace GestionalePalestra.Models;

public sealed class MembershipRecord
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int PlanId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPaid { get; set; }
    public string Notes { get; set; } = string.Empty;

    public string PaymentLabel => IsPaid ? "Pagato" : "Da saldare";
    public string Status => EndDate.Date >= DateTime.Today ? "Attivo" : "Scaduto";
}
