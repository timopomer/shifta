namespace Backend.Api.Entities;

public class EmployeePreference
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public PreferenceType Type { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public bool IsHard { get; set; }
    public DateTime CreatedAt { get; set; }
}
