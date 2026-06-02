using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections.Specialized;
using GestionalePalestra.Data;
using GestionalePalestra.Infrastructure;
using GestionalePalestra.Models;

namespace GestionalePalestra.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly GymRepository repository;

    private Member? selectedMember;
    private Member? selectedMembershipMember;
    private Member? selectedPaymentMember;
    private Member? selectedAttendanceMember;
    private MembershipPlan? selectedPlan;
    private MembershipRecord? selectedExpiringMembership;

    private string memberFormTitle = "Nuovo iscritto";
    private string memberFullName = string.Empty;
    private string memberPhone = string.Empty;
    private string memberEmail = string.Empty;
    private string memberJoinDateText = DateTime.Today.ToString("yyyy-MM-dd");
    private bool memberIsActive = true;

    private string membershipStartDateText = DateTime.Today.ToString("yyyy-MM-dd");
    private bool membershipIsPaid = true;
    private string membershipNotes = string.Empty;

    private string paymentAmountText = string.Empty;
    private string paymentMethod = "Contanti";
    private string paymentNote = string.Empty;

    private int totalMembers;
    private int activeMemberships;
    private decimal monthlyRevenue;
    private decimal totalRevenue;
    private int todayAttendances;
    private int expiringSoonCount;
    private string latestPaymentSummary = "Nessun pagamento recente.";
    private string latestAttendanceSummary = "Nessun check-in recente.";
    private bool isRefreshingCollections;

    public MainViewModel(GymRepository repository)
    {
        this.repository = repository;

        AttachCollectionHandlers();

        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        SaveMemberCommand = new RelayCommand(async _ => await SaveMemberAsync());
        ResetMemberFormCommand = new RelayCommand(_ => ResetMemberForm());
        ToggleMemberActiveCommand = new RelayCommand(_ => ToggleMemberActive());
        DeleteMemberCommand = new RelayCommand(async _ => await DeleteSelectedMemberAsync(), _ => SelectedMember is not null);
        AddMembershipCommand = new RelayCommand(async _ => await AddMembershipAsync(), _ => SelectedMembershipMember is not null && SelectedPlan is not null);
        AddPaymentCommand = new RelayCommand(async _ => await AddPaymentAsync(), _ => SelectedPaymentMember is not null);
        AddAttendanceCommand = new RelayCommand(async _ => await AddAttendanceAsync(), _ => SelectedAttendanceMember is not null);
    }

    public ObservableCollection<Member> Members { get; } = new();
    public ObservableCollection<MembershipPlan> Plans { get; } = new();
    public ObservableCollection<MembershipRecord> Memberships { get; } = new();
    public ObservableCollection<PaymentRecord> Payments { get; } = new();
    public ObservableCollection<AttendanceRecord> Attendances { get; } = new();
    public ObservableCollection<MembershipRecord> ExpiringMemberships { get; } = new();
    public ObservableCollection<PaymentRecord> RecentPayments { get; } = new();
    public ObservableCollection<AttendanceRecord> RecentAttendances { get; } = new();

    public RelayCommand RefreshCommand { get; }
    public RelayCommand SaveMemberCommand { get; }
    public RelayCommand ResetMemberFormCommand { get; }
    public RelayCommand ToggleMemberActiveCommand { get; }
    public RelayCommand DeleteMemberCommand { get; }
    public RelayCommand AddMembershipCommand { get; }
    public RelayCommand AddPaymentCommand { get; }
    public RelayCommand AddAttendanceCommand { get; }

    public int TotalMembers
    {
        get => totalMembers;
        set => SetProperty(ref totalMembers, value);
    }

    public int ActiveMemberships
    {
        get => activeMemberships;
        set => SetProperty(ref activeMemberships, value);
    }

    public decimal MonthlyRevenue
    {
        get => monthlyRevenue;
        set => SetProperty(ref monthlyRevenue, value);
    }

    public decimal TotalRevenue
    {
        get => totalRevenue;
        set => SetProperty(ref totalRevenue, value);
    }

    public int TodayAttendances
    {
        get => todayAttendances;
        set => SetProperty(ref todayAttendances, value);
    }

    public int ExpiringSoonCount
    {
        get => expiringSoonCount;
        set => SetProperty(ref expiringSoonCount, value);
    }

    public string LatestPaymentSummary
    {
        get => latestPaymentSummary;
        set => SetProperty(ref latestPaymentSummary, value);
    }

    public string LatestAttendanceSummary
    {
        get => latestAttendanceSummary;
        set => SetProperty(ref latestAttendanceSummary, value);
    }

    public string MemberFormTitle
    {
        get => memberFormTitle;
        set => SetProperty(ref memberFormTitle, value);
    }

    public string MemberFullName
    {
        get => memberFullName;
        set => SetProperty(ref memberFullName, value);
    }

    public string MemberPhone
    {
        get => memberPhone;
        set => SetProperty(ref memberPhone, value);
    }

    public string MemberEmail
    {
        get => memberEmail;
        set => SetProperty(ref memberEmail, value);
    }

    public string MemberJoinDateText
    {
        get => memberJoinDateText;
        set => SetProperty(ref memberJoinDateText, value);
    }

    public bool MemberIsActive
    {
        get => memberIsActive;
        set
        {
            if (SetProperty(ref memberIsActive, value))
            {
                OnPropertyChanged(nameof(MemberActiveLabel));
            }
        }
    }

    public string MemberActiveLabel => MemberIsActive ? "Attivo" : "Non attivo";

    public string MembershipStartDateText
    {
        get => membershipStartDateText;
        set => SetProperty(ref membershipStartDateText, value);
    }

    public bool MembershipIsPaid
    {
        get => membershipIsPaid;
        set => SetProperty(ref membershipIsPaid, value);
    }

    public string MembershipNotes
    {
        get => membershipNotes;
        set => SetProperty(ref membershipNotes, value);
    }

    public string PaymentAmountText
    {
        get => paymentAmountText;
        set => SetProperty(ref paymentAmountText, value);
    }

    public string PaymentMethod
    {
        get => paymentMethod;
        set => SetProperty(ref paymentMethod, value);
    }

    public string PaymentNote
    {
        get => paymentNote;
        set => SetProperty(ref paymentNote, value);
    }

    public Member? SelectedMember
    {
        get => selectedMember;
        set
        {
            if (SetProperty(ref selectedMember, value))
            {
                if (value is not null)
                {
                    PopulateMemberForm(value);
                }

                DeleteMemberCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Member? SelectedMembershipMember
    {
        get => selectedMembershipMember;
        set
        {
            if (SetProperty(ref selectedMembershipMember, value))
            {
                AddMembershipCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Member? SelectedPaymentMember
    {
        get => selectedPaymentMember;
        set
        {
            if (SetProperty(ref selectedPaymentMember, value))
            {
                AddPaymentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public Member? SelectedAttendanceMember
    {
        get => selectedAttendanceMember;
        set
        {
            if (SetProperty(ref selectedAttendanceMember, value))
            {
                AddAttendanceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public MembershipPlan? SelectedPlan
    {
        get => selectedPlan;
        set
        {
            if (SetProperty(ref selectedPlan, value))
            {
                AddMembershipCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public MembershipRecord? SelectedExpiringMembership
    {
        get => selectedExpiringMembership;
        set => SetProperty(ref selectedExpiringMembership, value);
    }

    public async Task LoadAsync()
    {
        isRefreshingCollections = true;

        Members.Clear();
        foreach (var member in await repository.GetMembersAsync())
        {
            Members.Add(member);
        }

        Plans.Clear();
        foreach (var plan in await repository.GetPlansAsync())
        {
            Plans.Add(plan);
        }

        Memberships.Clear();
        foreach (var membership in await repository.GetMembershipsAsync())
        {
            Memberships.Add(membership);
        }

        Payments.Clear();
        foreach (var payment in await repository.GetPaymentsAsync())
        {
            Payments.Add(payment);
        }

        Attendances.Clear();
        foreach (var attendance in await repository.GetAttendancesAsync())
        {
            Attendances.Add(attendance);
        }

        ExpiringMemberships.Clear();
        foreach (var item in await repository.GetExpiringMembershipsAsync(14))
        {
            ExpiringMemberships.Add(item);
        }

        ResetMemberForm();
        SelectedMembershipMember ??= Members.FirstOrDefault();
        SelectedPaymentMember ??= Members.FirstOrDefault();
        SelectedAttendanceMember ??= Members.FirstOrDefault();
        SelectedPlan ??= Plans.FirstOrDefault();

        isRefreshingCollections = false;
        RefreshDashboardState();
    }

    private async Task SaveMemberAsync()
    {
        if (string.IsNullOrWhiteSpace(MemberFullName))
        {
            return;
        }

        var member = SelectedMember ?? new Member();
        member.FullName = MemberFullName.Trim();
        member.Phone = MemberPhone.Trim();
        member.Email = MemberEmail.Trim();
        member.JoinDate = ParseDate(MemberJoinDateText, DateTime.Today);
        member.IsActive = MemberIsActive;

        await repository.SaveMemberAsync(member);
        SelectedMember = null;
        await LoadAsync();
    }

    private async Task DeleteSelectedMemberAsync()
    {
        if (SelectedMember is null)
        {
            return;
        }

        await repository.DeleteMemberAsync(SelectedMember.Id);
        SelectedMember = null;
        await LoadAsync();
    }

    private async Task AddMembershipAsync()
    {
        if (SelectedMembershipMember is null || SelectedPlan is null)
        {
            return;
        }

        var startDate = ParseDate(MembershipStartDateText, DateTime.Today);
        var endDate = startDate.AddMonths(SelectedPlan.Months);

        var record = new MembershipRecord
        {
            MemberId = SelectedMembershipMember.Id,
            PlanId = SelectedPlan.Id,
            StartDate = startDate,
            EndDate = endDate,
            IsPaid = MembershipIsPaid,
            Notes = MembershipNotes.Trim()
        };

        await repository.SaveMembershipAsync(record);
        MembershipNotes = string.Empty;
        await LoadAsync();
    }

    private async Task AddPaymentAsync()
    {
        if (SelectedPaymentMember is null)
        {
            return;
        }

        if (!decimal.TryParse(PaymentAmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
        {
            return;
        }

        var payment = new PaymentRecord
        {
            MemberId = SelectedPaymentMember.Id,
            Amount = amount,
            PaymentDate = DateTime.Today,
            Method = string.IsNullOrWhiteSpace(PaymentMethod) ? "Contanti" : PaymentMethod.Trim(),
            Note = PaymentNote.Trim()
        };

        await repository.AddPaymentAsync(payment);
        PaymentAmountText = string.Empty;
        PaymentNote = string.Empty;
        await LoadAsync();
    }

    private async Task AddAttendanceAsync()
    {
        if (SelectedAttendanceMember is null)
        {
            return;
        }

        await repository.AddAttendanceAsync(new AttendanceRecord
        {
            MemberId = SelectedAttendanceMember.Id,
            CheckInTime = DateTime.Now
        });

        await LoadAsync();
    }

    private void PopulateMemberForm(Member member)
    {
        MemberFormTitle = $"Modifica iscritto #{member.Id}";
        MemberFullName = member.FullName;
        MemberPhone = member.Phone;
        MemberEmail = member.Email;
        MemberJoinDateText = member.JoinDate.ToString("yyyy-MM-dd");
        MemberIsActive = member.IsActive;
    }

    private void ResetMemberForm()
    {
        MemberFormTitle = "Nuovo iscritto";
        MemberFullName = string.Empty;
        MemberPhone = string.Empty;
        MemberEmail = string.Empty;
        MemberJoinDateText = DateTime.Today.ToString("yyyy-MM-dd");
        MemberIsActive = true;
        SelectedMember = null;
    }

    private void ToggleMemberActive()
    {
        MemberIsActive = !MemberIsActive;
    }

    private void AttachCollectionHandlers()
    {
        Members.CollectionChanged += OnObservableCollectionChanged;
        Plans.CollectionChanged += OnObservableCollectionChanged;
        Memberships.CollectionChanged += OnObservableCollectionChanged;
        Payments.CollectionChanged += OnObservableCollectionChanged;
        Attendances.CollectionChanged += OnObservableCollectionChanged;
        ExpiringMemberships.CollectionChanged += OnObservableCollectionChanged;
        RecentPayments.CollectionChanged += OnObservableCollectionChanged;
        RecentAttendances.CollectionChanged += OnObservableCollectionChanged;
    }

    private void OnObservableCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!isRefreshingCollections)
        {
            RefreshDashboardState();
        }
    }

    private void RefreshDashboardState()
    {
        isRefreshingCollections = true;
        try
        {
            RecentPayments.Clear();
            foreach (var payment in Payments.Take(5))
            {
                RecentPayments.Add(payment);
            }

            RecentAttendances.Clear();
            foreach (var attendance in Attendances.Take(5))
            {
                RecentAttendances.Add(attendance);
            }

            TotalMembers = Members.Count;
            ActiveMemberships = Memberships.Count(membership => membership.EndDate.Date >= DateTime.Today);
            MonthlyRevenue = Payments
                .Where(payment => payment.PaymentDate.Year == DateTime.Today.Year && payment.PaymentDate.Month == DateTime.Today.Month)
                .Sum(payment => payment.Amount);
            TotalRevenue = Payments.Sum(payment => payment.Amount);
            TodayAttendances = Attendances.Count(attendance => attendance.CheckInTime.Date == DateTime.Today);
            ExpiringSoonCount = ExpiringMemberships.Count;
            LatestPaymentSummary = Payments.FirstOrDefault() is PaymentRecord latestPayment
                ? $"{latestPayment.MemberName} - {latestPayment.Amount:N2} € il {latestPayment.PaymentDate:dd/MM/yyyy}"
                : "Nessun pagamento recente.";
            LatestAttendanceSummary = Attendances.FirstOrDefault() is AttendanceRecord latestAttendance
                ? $"{latestAttendance.MemberName} alle {latestAttendance.CheckInTime:HH:mm} del {latestAttendance.CheckInTime:dd/MM/yyyy}"
                : "Nessun check-in recente.";
        }
        finally
        {
            isRefreshingCollections = false;
        }
    }

    private static DateTime ParseDate(string input, DateTime fallback)
    {
        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed.Date;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed.Date;
        }

        return fallback;
    }
}
