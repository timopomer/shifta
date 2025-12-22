using Backend.Api.Entities;

namespace Backend.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Only seed if database is empty
        if (context.Employees.Any())
            return;

        var now = DateTime.UtcNow;
        
        // Get the Monday of the current week
        var today = DateTime.UtcNow.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && today.DayOfWeek != DayOfWeek.Monday)
            daysUntilMonday = 7;
        var thisMonday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday)
            thisMonday = thisMonday.AddDays(-7);
        var nextMonday = thisMonday.AddDays(7);

        // Create employees
        var employees = new List<Employee>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Alex Rivera",
                Email = "alex.rivera@example.com",
                Abilities = ["barista", "cashier"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Jordan Chen",
                Email = "jordan.chen@example.com",
                Abilities = ["barista", "inventory"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Sam Okonkwo",
                Email = "sam.okonkwo@example.com",
                Abilities = ["cashier", "customer-service"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Taylor Kim",
                Email = "taylor.kim@example.com",
                Abilities = ["barista", "cashier", "closing"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Morgan Bailey",
                Email = "morgan.bailey@example.com",
                Abilities = ["barista", "opening", "inventory"],
                IsManager = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Casey Martinez",
                Email = "casey.martinez@example.com",
                Abilities = ["cashier", "customer-service", "closing"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Drew Patel",
                Email = "drew.patel@example.com",
                Abilities = ["barista", "opening"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Riley Thompson",
                Email = "riley.thompson@example.com",
                Abilities = ["cashier", "inventory"],
                IsManager = false,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Employees.AddRange(employees);

        // Create schedules
        var currentSchedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week of " + thisMonday.ToString("MMM d"),
            WeekStartDate = thisMonday,
            Status = ScheduleStatus.Finalized,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-2),
            FinalizedAt = now.AddDays(-2)
        };

        var nextSchedule = new ShiftSchedule
        {
            Id = Guid.NewGuid(),
            Name = "Week of " + nextMonday.ToString("MMM d"),
            WeekStartDate = nextMonday,
            Status = ScheduleStatus.OpenForPreferences,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now
        };

        context.ShiftSchedules.AddRange(currentSchedule, nextSchedule);

        // Create shifts for current week (finalized schedule with assignments)
        var currentWeekShifts = new List<Shift>();
        var assignments = new List<ShiftAssignment>();
        var random = new Random(42); // Deterministic for consistent demo

        for (int day = 0; day < 7; day++)
        {
            var shiftDate = thisMonday.AddDays(day);
            var dayName = shiftDate.DayOfWeek.ToString();
            
            // Morning shift (6am - 2pm)
            var morningShift = new Shift
            {
                Id = Guid.NewGuid(),
                ShiftScheduleId = currentSchedule.Id,
                Name = $"{dayName} Morning",
                StartTime = shiftDate.AddHours(6),
                EndTime = shiftDate.AddHours(14),
                RequiredAbilities = ["opening", "barista"],
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7)
            };
            currentWeekShifts.Add(morningShift);

            // Afternoon shift (2pm - 10pm)
            var afternoonShift = new Shift
            {
                Id = Guid.NewGuid(),
                ShiftScheduleId = currentSchedule.Id,
                Name = $"{dayName} Afternoon",
                StartTime = shiftDate.AddHours(14),
                EndTime = shiftDate.AddHours(22),
                RequiredAbilities = ["closing", "cashier"],
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7)
            };
            currentWeekShifts.Add(afternoonShift);

            // Assign employees to these shifts
            var availableEmployees = employees.ToList();
            
            var morningEmployee = availableEmployees[random.Next(availableEmployees.Count)];
            assignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                ShiftId = morningShift.Id,
                EmployeeId = morningEmployee.Id,
                AssignedAt = now.AddDays(-2)
            });
            availableEmployees.Remove(morningEmployee);

            var afternoonEmployee = availableEmployees[random.Next(availableEmployees.Count)];
            assignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                ShiftId = afternoonShift.Id,
                EmployeeId = afternoonEmployee.Id,
                AssignedAt = now.AddDays(-2)
            });
        }

        context.Shifts.AddRange(currentWeekShifts);
        context.ShiftAssignments.AddRange(assignments);

        // Create shifts for next week (open for preferences)
        var nextWeekShifts = new List<Shift>();
        
        for (int day = 0; day < 7; day++)
        {
            var shiftDate = nextMonday.AddDays(day);
            var dayName = shiftDate.DayOfWeek.ToString();
            
            // Morning shift
            nextWeekShifts.Add(new Shift
            {
                Id = Guid.NewGuid(),
                ShiftScheduleId = nextSchedule.Id,
                Name = $"{dayName} Morning",
                StartTime = shiftDate.AddHours(6),
                EndTime = shiftDate.AddHours(14),
                RequiredAbilities = ["opening", "barista"],
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });

            // Afternoon shift
            nextWeekShifts.Add(new Shift
            {
                Id = Guid.NewGuid(),
                ShiftScheduleId = nextSchedule.Id,
                Name = $"{dayName} Afternoon",
                StartTime = shiftDate.AddHours(14),
                EndTime = shiftDate.AddHours(22),
                RequiredAbilities = ["closing", "cashier"],
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });
        }

        context.Shifts.AddRange(nextWeekShifts);

        // Add some preferences for next week's schedule
        var preferences = new List<EmployeePreference>
        {
            // Alex prefers Monday morning
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[0].Id,
                ShiftId = nextWeekShifts[0].Id, // Monday Morning
                Type = PreferenceType.PreferShift,
                IsHard = false,
                CreatedAt = now
            },
            // Jordan is unavailable Tuesday afternoon
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[1].Id,
                ShiftId = nextWeekShifts[3].Id, // Tuesday Afternoon
                Type = PreferenceType.Unavailable,
                IsHard = true,
                CreatedAt = now
            },
            // Sam prefers Wednesday shifts
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[2].Id,
                ShiftId = nextWeekShifts[4].Id, // Wednesday Morning
                Type = PreferenceType.PreferShift,
                IsHard = false,
                CreatedAt = now
            },
            // Taylor prefers closing shifts (Friday afternoon)
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[3].Id,
                ShiftId = nextWeekShifts[9].Id, // Friday Afternoon
                Type = PreferenceType.PreferShift,
                IsHard = false,
                CreatedAt = now
            },
            // Casey is unavailable Saturday
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[5].Id,
                ShiftId = nextWeekShifts[10].Id, // Saturday Morning
                Type = PreferenceType.Unavailable,
                IsHard = true,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[5].Id,
                ShiftId = nextWeekShifts[11].Id, // Saturday Afternoon
                Type = PreferenceType.Unavailable,
                IsHard = true,
                CreatedAt = now
            },
            // Drew prefers morning shifts
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[6].Id,
                ShiftId = nextWeekShifts[0].Id, // Monday Morning
                Type = PreferenceType.PreferShift,
                IsHard = false,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[6].Id,
                ShiftId = nextWeekShifts[6].Id, // Thursday Morning
                Type = PreferenceType.PreferShift,
                IsHard = false,
                CreatedAt = now
            }
        };

        context.EmployeePreferences.AddRange(preferences);

        // Create manager-employee relationships
        // Morgan Bailey (index 4) is the manager
        var morganId = employees[4].Id;
        var managerEmployees = new List<ManagerEmployee>();
        
        // Assign all non-manager employees to Morgan
        for (int i = 0; i < employees.Count; i++)
        {
            if (i != 4) // Skip Morgan (the manager)
            {
                managerEmployees.Add(new ManagerEmployee
                {
                    Id = Guid.NewGuid(),
                    ManagerId = morganId,
                    EmployeeId = employees[i].Id,
                    CreatedAt = now
                });
            }
        }

        context.ManagerEmployees.AddRange(managerEmployees);

        // Create some shift requests for next week's schedule
        var shiftRequests = new List<ShiftRequest>
        {
            // Alex wants to work Monday morning
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[0].Id, // Alex
                ShiftId = nextWeekShifts[0].Id, // Monday Morning
                RequestType = ShiftRequestType.WantToWork,
                Status = RequestStatus.Pending,
                Note = "I'd love to take this shift!",
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now.AddHours(-2)
            },
            // Sam doesn't want to work Saturday afternoon
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[2].Id, // Sam
                ShiftId = nextWeekShifts[11].Id, // Saturday Afternoon
                RequestType = ShiftRequestType.DoNotWantToWork,
                Status = RequestStatus.Pending,
                Note = "I have a family event that day",
                CreatedAt = now.AddHours(-1),
                UpdatedAt = now.AddHours(-1)
            },
            // Drew wants to work Thursday morning (already approved)
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[6].Id, // Drew
                ShiftId = nextWeekShifts[6].Id, // Thursday Morning
                RequestType = ShiftRequestType.WantToWork,
                Status = RequestStatus.Approved,
                Note = "This works great with my schedule",
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddHours(-12),
                ReviewedById = morganId,
                ReviewedAt = now.AddHours(-12),
                ReviewNote = "Approved - Drew has been reliable on morning shifts"
            }
        };

        context.ShiftRequests.AddRange(shiftRequests);

        // Create some time off requests
        var timeOffRequests = new List<TimeOffRequest>
        {
            // Taylor requesting time off next week Friday
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[3].Id, // Taylor
                StartDate = nextMonday.AddDays(4), // Friday
                EndDate = nextMonday.AddDays(5), // Saturday (exclusive end)
                Status = RequestStatus.Pending,
                Reason = "Doctor's appointment",
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-3)
            },
            // Riley time off already approved
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[7].Id, // Riley
                StartDate = nextMonday.AddDays(7), // Next next Monday
                EndDate = nextMonday.AddDays(10), // Thursday
                Status = RequestStatus.Approved,
                Reason = "Family vacation",
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-1),
                ReviewedById = morganId,
                ReviewedAt = now.AddDays(-1),
                ReviewNote = "Enjoy your vacation!"
            },
            // Jordan time off request rejected
            new()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employees[1].Id, // Jordan
                StartDate = thisMonday.AddDays(2), // Wednesday of current week
                EndDate = thisMonday.AddDays(3),
                Status = RequestStatus.Rejected,
                Reason = "Personal day",
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-4),
                ReviewedById = morganId,
                ReviewedAt = now.AddDays(-4),
                ReviewNote = "Sorry, we're short-staffed this day"
            }
        };

        context.TimeOffRequests.AddRange(timeOffRequests);

        await context.SaveChangesAsync();
    }
}

