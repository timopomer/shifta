namespace Backend.Api.Entities;

public enum ScheduleStatus
{
    Draft = 0,
    OpenForPreferences = 1,
    PendingReview = 2,
    Finalized = 3,
    Archived = 4
}
