using System.Globalization;
using System.IO;
using GestionalePalestra.Models;
using Microsoft.Data.Sqlite;

namespace GestionalePalestra.Data;

public sealed class GymRepository
{
    private readonly string databasePath;

    public GymRepository(string databasePath)
    {
        this.databasePath = databasePath;
    }

    private SqliteConnection CreateConnection() => new($"Data Source={databasePath}");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var createMembers = connection.CreateCommand();
        createMembers.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Members (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT NOT NULL,
                JoinDate TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );
            """;
        await createMembers.ExecuteNonQueryAsync();

        var createPlans = connection.CreateCommand();
        createPlans.CommandText =
            """
            CREATE TABLE IF NOT EXISTS MembershipPlans (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Months INTEGER NOT NULL,
                Price REAL NOT NULL,
                Description TEXT NOT NULL
            );
            """;
        await createPlans.ExecuteNonQueryAsync();

        var createMemberships = connection.CreateCommand();
        createMemberships.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Memberships (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MemberId INTEGER NOT NULL,
                PlanId INTEGER NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                IsPaid INTEGER NOT NULL,
                Notes TEXT NOT NULL
            );
            """;
        await createMemberships.ExecuteNonQueryAsync();

        var createPayments = connection.CreateCommand();
        createPayments.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Payments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MemberId INTEGER NOT NULL,
                Amount REAL NOT NULL,
                PaymentDate TEXT NOT NULL,
                Method TEXT NOT NULL,
                Note TEXT NOT NULL
            );
            """;
        await createPayments.ExecuteNonQueryAsync();

        var createAttendances = connection.CreateCommand();
        createAttendances.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Attendances (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MemberId INTEGER NOT NULL,
                CheckInTime TEXT NOT NULL
            );
            """;
        await createAttendances.ExecuteNonQueryAsync();

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM MembershipPlans;";
        var plansCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        if (plansCount == 0)
        {
            var seedPlans = new[]
            {
                new MembershipPlan { Name = "Mensile", Months = 1, Price = 35, Description = "Piano base da 30 giorni" },
                new MembershipPlan { Name = "Trimestrale", Months = 3, Price = 90, Description = "Soluzione intermedia" },
                new MembershipPlan { Name = "Annuale", Months = 12, Price = 320, Description = "Formula annuale" }
            };

            foreach (var plan in seedPlans)
            {
                await SavePlanAsync(plan);
            }
        }

        var membersCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Members;");
        if (membersCount == 0)
        {
            var seedMembers = new[]
            {
                new Member { FullName = "Marco Rossi", Phone = "+39 333 100 2001", Email = "marco.rossi@example.com", JoinDate = DateTime.Today.AddMonths(-4), IsActive = true },
                new Member { FullName = "Giulia Bianchi", Phone = "+39 333 100 2002", Email = "giulia.bianchi@example.com", JoinDate = DateTime.Today.AddMonths(-2), IsActive = true },
                new Member { FullName = "Luca Conti", Phone = "+39 333 100 2003", Email = "luca.conti@example.com", JoinDate = DateTime.Today.AddMonths(-7), IsActive = true },
                new Member { FullName = "Sara Ferri", Phone = "+39 333 100 2004", Email = "sara.ferri@example.com", JoinDate = DateTime.Today.AddMonths(-1), IsActive = false }
            };

            foreach (var member in seedMembers)
            {
                await SaveMemberAsync(member);
            }
        }

        var membershipCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Memberships;");
        var paymentCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Payments;");
        var attendanceCount = await GetCountAsync(connection, "SELECT COUNT(*) FROM Attendances;");

        if (membershipCount == 0 && paymentCount == 0 && attendanceCount == 0)
        {
            var members = (await GetMembersAsync()).ToDictionary(member => member.FullName);
            var plans = (await GetPlansAsync()).ToDictionary(plan => plan.Name);

            await SaveMembershipAsync(new MembershipRecord
            {
                MemberId = members["Marco Rossi"].Id,
                PlanId = plans["Annuale"].Id,
                StartDate = DateTime.Today.AddMonths(-2),
                EndDate = DateTime.Today.AddMonths(10),
                IsPaid = true,
                Notes = "Upgrade annuale con accesso completo."
            });

            await SaveMembershipAsync(new MembershipRecord
            {
                MemberId = members["Giulia Bianchi"].Id,
                PlanId = plans["Trimestrale"].Id,
                StartDate = DateTime.Today.AddDays(-12),
                EndDate = DateTime.Today.AddMonths(3).AddDays(-12),
                IsPaid = true,
                Notes = "Promo primavera."
            });

            await SaveMembershipAsync(new MembershipRecord
            {
                MemberId = members["Luca Conti"].Id,
                PlanId = plans["Mensile"].Id,
                StartDate = DateTime.Today.AddDays(-5),
                EndDate = DateTime.Today.AddDays(25),
                IsPaid = false,
                Notes = "Rinnovo in sospeso."
            });

            await AddPaymentAsync(new PaymentRecord
            {
                MemberId = members["Marco Rossi"].Id,
                Amount = 320,
                PaymentDate = DateTime.Today.AddDays(-8),
                Method = "Carta",
                Note = "Quota annuale"
            });

            await AddPaymentAsync(new PaymentRecord
            {
                MemberId = members["Giulia Bianchi"].Id,
                Amount = 90,
                PaymentDate = DateTime.Today.AddDays(-3),
                Method = "Contanti",
                Note = "Trimestrale"
            });

            await AddPaymentAsync(new PaymentRecord
            {
                MemberId = members["Luca Conti"].Id,
                Amount = 35,
                PaymentDate = DateTime.Today,
                Method = "POS",
                Note = "Mensile"
            });

            await AddAttendanceAsync(new AttendanceRecord
            {
                MemberId = members["Marco Rossi"].Id,
                CheckInTime = DateTime.Now.AddHours(-2)
            });

            await AddAttendanceAsync(new AttendanceRecord
            {
                MemberId = members["Giulia Bianchi"].Id,
                CheckInTime = DateTime.Now.AddHours(-1).AddMinutes(-20)
            });
        }
    }

    public async Task<List<Member>> GetMembersAsync()
    {
        var members = new List<Member>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, Phone, Email, JoinDate, IsActive FROM Members ORDER BY FullName;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            members.Add(new Member
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.GetString(2),
                Email = reader.GetString(3),
                JoinDate = DateTime.Parse(reader.GetString(4), null, DateTimeStyles.RoundtripKind),
                IsActive = reader.GetInt32(5) == 1
            });
        }

        return members;
    }

    public async Task SaveMemberAsync(Member member)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        if (member.Id == 0)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO Members (FullName, Phone, Email, JoinDate, IsActive)
                VALUES ($fullName, $phone, $email, $joinDate, $isActive);
                """;
            command.Parameters.AddWithValue("$fullName", member.FullName.Trim());
            command.Parameters.AddWithValue("$phone", member.Phone.Trim());
            command.Parameters.AddWithValue("$email", member.Email.Trim());
            command.Parameters.AddWithValue("$joinDate", member.JoinDate.ToString("O"));
            command.Parameters.AddWithValue("$isActive", member.IsActive ? 1 : 0);
            await command.ExecuteNonQueryAsync();
            return;
        }

        var update = connection.CreateCommand();
        update.CommandText =
            """
            UPDATE Members
            SET FullName = $fullName,
                Phone = $phone,
                Email = $email,
                JoinDate = $joinDate,
                IsActive = $isActive
            WHERE Id = $id;
            """;
        update.Parameters.AddWithValue("$id", member.Id);
        update.Parameters.AddWithValue("$fullName", member.FullName.Trim());
        update.Parameters.AddWithValue("$phone", member.Phone.Trim());
        update.Parameters.AddWithValue("$email", member.Email.Trim());
        update.Parameters.AddWithValue("$joinDate", member.JoinDate.ToString("O"));
        update.Parameters.AddWithValue("$isActive", member.IsActive ? 1 : 0);
        await update.ExecuteNonQueryAsync();
    }

    public async Task DeleteMemberAsync(int memberId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Members WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", memberId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<MembershipPlan>> GetPlansAsync()
    {
        var plans = new List<MembershipPlan>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Months, Price, Description FROM MembershipPlans ORDER BY Months;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plans.Add(new MembershipPlan
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Months = reader.GetInt32(2),
                Price = Convert.ToDecimal(reader.GetDouble(3)),
                Description = reader.GetString(4)
            });
        }

        return plans;
    }

    public async Task SavePlanAsync(MembershipPlan plan)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO MembershipPlans (Name, Months, Price, Description)
            VALUES ($name, $months, $price, $description);
            """;
        command.Parameters.AddWithValue("$name", plan.Name);
        command.Parameters.AddWithValue("$months", plan.Months);
        command.Parameters.AddWithValue("$price", plan.Price);
        command.Parameters.AddWithValue("$description", plan.Description);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveMembershipAsync(MembershipRecord record)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Memberships (MemberId, PlanId, StartDate, EndDate, IsPaid, Notes)
            VALUES ($memberId, $planId, $startDate, $endDate, $isPaid, $notes);
            """;
        command.Parameters.AddWithValue("$memberId", record.MemberId);
        command.Parameters.AddWithValue("$planId", record.PlanId);
        command.Parameters.AddWithValue("$startDate", record.StartDate.ToString("O"));
        command.Parameters.AddWithValue("$endDate", record.EndDate.ToString("O"));
        command.Parameters.AddWithValue("$isPaid", record.IsPaid ? 1 : 0);
        command.Parameters.AddWithValue("$notes", record.Notes);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<MembershipRecord>> GetMembershipsAsync()
    {
        var memberships = new List<MembershipRecord>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT m.Id, m.MemberId, m.PlanId, mem.FullName, p.Name, m.StartDate, m.EndDate, m.IsPaid, m.Notes
            FROM Memberships m
            INNER JOIN Members mem ON mem.Id = m.MemberId
            INNER JOIN MembershipPlans p ON p.Id = m.PlanId
            ORDER BY m.EndDate DESC;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            memberships.Add(new MembershipRecord
            {
                Id = reader.GetInt32(0),
                MemberId = reader.GetInt32(1),
                PlanId = reader.GetInt32(2),
                MemberName = reader.GetString(3),
                PlanName = reader.GetString(4),
                StartDate = DateTime.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind),
                EndDate = DateTime.Parse(reader.GetString(6), null, DateTimeStyles.RoundtripKind),
                IsPaid = reader.GetInt32(7) == 1,
                Notes = reader.GetString(8)
            });
        }

        return memberships;
    }

    public async Task AddPaymentAsync(PaymentRecord record)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Payments (MemberId, Amount, PaymentDate, Method, Note)
            VALUES ($memberId, $amount, $paymentDate, $method, $note);
            """;
        command.Parameters.AddWithValue("$memberId", record.MemberId);
        command.Parameters.AddWithValue("$amount", record.Amount);
        command.Parameters.AddWithValue("$paymentDate", record.PaymentDate.ToString("O"));
        command.Parameters.AddWithValue("$method", record.Method);
        command.Parameters.AddWithValue("$note", record.Note);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<PaymentRecord>> GetPaymentsAsync()
    {
        var payments = new List<PaymentRecord>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT pay.Id, pay.MemberId, mem.FullName, pay.Amount, pay.PaymentDate, pay.Method, pay.Note
            FROM Payments pay
            INNER JOIN Members mem ON mem.Id = pay.MemberId
            ORDER BY pay.PaymentDate DESC;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            payments.Add(new PaymentRecord
            {
                Id = reader.GetInt32(0),
                MemberId = reader.GetInt32(1),
                MemberName = reader.GetString(2),
                Amount = Convert.ToDecimal(reader.GetDouble(3)),
                PaymentDate = DateTime.Parse(reader.GetString(4), null, DateTimeStyles.RoundtripKind),
                Method = reader.GetString(5),
                Note = reader.GetString(6)
            });
        }

        return payments;
    }

    public async Task AddAttendanceAsync(AttendanceRecord record)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Attendances (MemberId, CheckInTime)
            VALUES ($memberId, $checkInTime);
            """;
        command.Parameters.AddWithValue("$memberId", record.MemberId);
        command.Parameters.AddWithValue("$checkInTime", record.CheckInTime.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<AttendanceRecord>> GetAttendancesAsync()
    {
        var attendances = new List<AttendanceRecord>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT a.Id, a.MemberId, mem.FullName, a.CheckInTime
            FROM Attendances a
            INNER JOIN Members mem ON mem.Id = a.MemberId
            ORDER BY a.CheckInTime DESC;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            attendances.Add(new AttendanceRecord
            {
                Id = reader.GetInt32(0),
                MemberId = reader.GetInt32(1),
                MemberName = reader.GetString(2),
                CheckInTime = DateTime.Parse(reader.GetString(3), null, DateTimeStyles.RoundtripKind)
            });
        }

        return attendances;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var stats = new DashboardStats();

        var membersCommand = connection.CreateCommand();
        membersCommand.CommandText = "SELECT COUNT(*) FROM Members;";
        stats.TotalMembers = Convert.ToInt32(await membersCommand.ExecuteScalarAsync());

        var activeMembershipsCommand = connection.CreateCommand();
        activeMembershipsCommand.CommandText =
            """
            SELECT COUNT(*)
            FROM Memberships
            WHERE date(EndDate) >= date('now');
            """;
        stats.ActiveMemberships = Convert.ToInt32(await activeMembershipsCommand.ExecuteScalarAsync());

        var revenueCommand = connection.CreateCommand();
        revenueCommand.CommandText =
            """
            SELECT COALESCE(SUM(Amount), 0)
            FROM Payments
            WHERE strftime('%Y-%m', PaymentDate) = strftime('%Y-%m', 'now');
            """;
        stats.MonthlyRevenue = Convert.ToDecimal(await revenueCommand.ExecuteScalarAsync());

        var totalRevenueCommand = connection.CreateCommand();
        totalRevenueCommand.CommandText =
            """
            SELECT COALESCE(SUM(Amount), 0)
            FROM Payments;
            """;
        stats.TotalRevenue = Convert.ToDecimal(await totalRevenueCommand.ExecuteScalarAsync());

        var attendanceCommand = connection.CreateCommand();
        attendanceCommand.CommandText =
            """
            SELECT COUNT(*)
            FROM Attendances
            WHERE date(CheckInTime) = date('now');
            """;
        stats.TodayAttendances = Convert.ToInt32(await attendanceCommand.ExecuteScalarAsync());

        return stats;
    }

    public async Task<List<MembershipRecord>> GetExpiringMembershipsAsync(int daysAhead)
    {
        var expiring = new List<MembershipRecord>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT m.Id, m.MemberId, m.PlanId, mem.FullName, p.Name, m.StartDate, m.EndDate, m.IsPaid, m.Notes
            FROM Memberships m
            INNER JOIN Members mem ON mem.Id = m.MemberId
            INNER JOIN MembershipPlans p ON p.Id = m.PlanId
            WHERE date(m.EndDate) BETWEEN date('now') AND date('now', $daysAhead)
            ORDER BY m.EndDate ASC;
            """;
        command.Parameters.AddWithValue("$daysAhead", $"+{daysAhead} days");

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            expiring.Add(new MembershipRecord
            {
                Id = reader.GetInt32(0),
                MemberId = reader.GetInt32(1),
                PlanId = reader.GetInt32(2),
                MemberName = reader.GetString(3),
                PlanName = reader.GetString(4),
                StartDate = DateTime.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind),
                EndDate = DateTime.Parse(reader.GetString(6), null, DateTimeStyles.RoundtripKind),
                IsPaid = reader.GetInt32(7) == 1,
                Notes = reader.GetString(8)
            });
        }

        return expiring;
    }

    private static async Task<int> GetCountAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt32(value);
    }
}
