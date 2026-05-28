using MBappe.Common;
using MBappe.Models;
using MBappe.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MBappe.ViewModels.Learning;

public partial class LearningViewModel : ViewModelBase
{
    private readonly LearningService _learningService;
    private readonly EmployeeService _employeeService;
    private IReadOnlyDictionary<Guid, LearningCourse> _coursesById = new Dictionary<Guid, LearningCourse>();
    private IReadOnlyDictionary<Guid, EmployeeProfile> _employeesById = new Dictionary<Guid, EmployeeProfile>();

    [ObservableProperty]
    private ObservableCollection<LearningCourseRowViewModel> courses = [];

    [ObservableProperty]
    private ObservableCollection<LearningAssignmentRowViewModel> assignments = [];

    [ObservableProperty]
    private ObservableCollection<LearningEmployeeOption> employeeOptions = [];

    [ObservableProperty]
    private LearningCourseRowViewModel? selectedCourse;

    [ObservableProperty]
    private LearningAssignmentRowViewModel? selectedAssignment;

    [ObservableProperty]
    private bool isCourseDetailsActive = true;

    [ObservableProperty]
    private LearningEmployeeOption? selectedEmployeeOption;

    [ObservableProperty]
    private LearningFormatOption selectedNewFormat = LearningFormatOption.All[0];

    [ObservableProperty]
    private LearningFormatOption selectedEditFormat = LearningFormatOption.All[0];

    [ObservableProperty]
    private LearningCourseStatusOption selectedEditCourseStatus = LearningCourseStatusOption.All[0];

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string courseFormMessage = string.Empty;

    [ObservableProperty]
    private string assignmentFormMessage = string.Empty;

    [ObservableProperty]
    private string progressFormMessage = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isCreateCourseFormVisible;

    [ObservableProperty]
    private bool isEditCourseFormVisible;

    [ObservableProperty]
    private bool isAssignFormVisible;

    [ObservableProperty]
    private bool isProgressFormVisible;

    [ObservableProperty]
    private string newCourseTitle = string.Empty;

    [ObservableProperty]
    private string newCourseDescription = string.Empty;

    [ObservableProperty]
    private string newCourseProvider = string.Empty;

    [ObservableProperty]
    private string newCourseDurationText = string.Empty;

    [ObservableProperty]
    private string editCourseTitle = string.Empty;

    [ObservableProperty]
    private string editCourseDescription = string.Empty;

    [ObservableProperty]
    private string editCourseProvider = string.Empty;

    [ObservableProperty]
    private string editCourseDurationText = string.Empty;

    [ObservableProperty]
    private string assignmentDueDateText = string.Empty;

    [ObservableProperty]
    private string progressPercentText = string.Empty;

    [ObservableProperty]
    private string progressScoreText = string.Empty;

    [ObservableProperty]
    private int totalCourseCount;

    [ObservableProperty]
    private int activeCourseCount;

    [ObservableProperty]
    private int activeAssignmentCount;

    [ObservableProperty]
    private int completedAssignmentCount;

    public bool CanManageCourses => _learningService.CanManageCourses();

    public bool CanAssignLearning => _learningService.CanAssignLearning();

    public bool CanUpdateLearningProgress => _learningService.CanUpdateLearningProgress();

    public bool HasSelectedCourse => SelectedCourse is not null;

    public bool HasSelectedAssignment => SelectedAssignment is not null;

    public bool HasNoSelectedCourse => SelectedCourse is null;

    public bool HasNoSelectedAssignment => SelectedAssignment is null;

    public bool CanShowCourseActions => CanManageCourses && HasSelectedCourse;

    public bool CanShowAssignmentActions => HasSelectedAssignment && CanUpdateLearningProgress;

    public bool CanShowAssignAction => CanAssignLearning && HasSelectedCourse;

    public bool CanShowCourseDetails =>
        HasSelectedCourse
        && IsCourseDetailsActive
        && !IsCreateCourseFormVisible
        && !IsEditCourseFormVisible
        && !IsAssignFormVisible
        && !IsProgressFormVisible;

    public bool CanShowAssignmentDetails =>
        HasSelectedAssignment
        && !IsCourseDetailsActive
        && !IsCreateCourseFormVisible
        && !IsEditCourseFormVisible
        && !IsAssignFormVisible
        && !IsProgressFormVisible;

    public bool CanShowCoursePrompt =>
        HasNoSelectedCourse
        && !IsCreateCourseFormVisible
        && !IsEditCourseFormVisible
        && !IsAssignFormVisible
        && !IsProgressFormVisible;

    public bool CanShowAssignmentPrompt =>
        HasNoSelectedAssignment
        && !IsCreateCourseFormVisible
        && !IsEditCourseFormVisible
        && !IsAssignFormVisible
        && !IsProgressFormVisible;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasCourseFormMessage => !string.IsNullOrWhiteSpace(CourseFormMessage);

    public bool HasAssignmentFormMessage => !string.IsNullOrWhiteSpace(AssignmentFormMessage);

    public bool HasProgressFormMessage => !string.IsNullOrWhiteSpace(ProgressFormMessage);

    public IReadOnlyList<LearningFormatOption> FormatOptions { get; } = LearningFormatOption.All;

    public IReadOnlyList<LearningCourseStatusOption> CourseStatusOptions { get; } = LearningCourseStatusOption.All;

    public string SelectedCourseCaption => SelectedCourse is null
        ? "Курс не выбран"
        : SelectedCourse.Title;

    public string SelectedAssignmentCaption => SelectedAssignment is null
        ? "Назначение не выбрано"
        : $"{SelectedAssignment.CourseTitle} · {SelectedAssignment.EmployeeTitle}";

    public LearningViewModel(
        LearningService learningService,
        EmployeeService employeeService)
    {
        _learningService = learningService;
        _employeeService = employeeService;

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        HideFormsAfterSelectionChanged();

        IsBusy = true;
        var selectedCourseId = SelectedCourse?.Course.Id;
        var selectedAssignmentId = SelectedAssignment?.Assignment.Id;

        await LoadEmployeesAsync();

        var coursesResult = await _learningService.GetVisibleCoursesAsync();
        var assignmentsResult = await _learningService.GetVisibleAssignmentsAsync();

        IsBusy = false;

        if (!coursesResult.Success || coursesResult.Courses is null)
        {
            StatusMessage = coursesResult.Message;
            return;
        }

        if (!assignmentsResult.Success || assignmentsResult.Assignments is null)
        {
            StatusMessage = assignmentsResult.Message;
            return;
        }

        _coursesById = coursesResult.Courses.ToDictionary(course => course.Id);

        Courses = new ObservableCollection<LearningCourseRowViewModel>(
            coursesResult.Courses.Select(course => new LearningCourseRowViewModel(course)));

        Assignments = new ObservableCollection<LearningAssignmentRowViewModel>(
            assignmentsResult.Assignments.Select(assignment =>
            {
                _coursesById.TryGetValue(assignment.CourseId, out var course);
                _employeesById.TryGetValue(assignment.EmployeeId, out var employee);

                return new LearningAssignmentRowViewModel(assignment, course, employee);
            }));

        if (selectedAssignmentId is not null)
        {
            SelectedAssignment = Assignments.FirstOrDefault(assignment => assignment.Assignment.Id == selectedAssignmentId.Value);
            SelectedCourse = null;
        }
        else
        {
            SelectedCourse = selectedCourseId is null
            ? Courses.FirstOrDefault()
            : Courses.FirstOrDefault(course => course.Course.Id == selectedCourseId.Value) ?? Courses.FirstOrDefault();
            SelectedAssignment = null;
        }

        UpdateSummary();
        StatusMessage = "Модуль обучения обновлен";
    }

    [RelayCommand]
    private void OpenCreateCourseForm()
    {
        if (!CanManageCourses)
            return;

        ClearMessages();
        ClearCreateCourseForm();
        IsCreateCourseFormVisible = true;
        IsEditCourseFormVisible = false;
        IsAssignFormVisible = false;
        IsProgressFormVisible = false;
    }

    [RelayCommand]
    private void OpenEditCourseForm()
    {
        if (!CanManageCourses)
            return;

        if (SelectedCourse is null)
        {
            StatusMessage = "Выберите курс";
            return;
        }

        ClearMessages();

        var course = SelectedCourse.Course;
        EditCourseTitle = course.Title;
        EditCourseDescription = course.Description;
        EditCourseProvider = course.Provider;
        EditCourseDurationText = FormatNumber(course.DurationHours);
        SelectedEditFormat = FormatOptions.First(option => option.Format == course.Format);
        SelectedEditCourseStatus = CourseStatusOptions.First(option => option.Status == course.Status);

        IsCreateCourseFormVisible = false;
        IsEditCourseFormVisible = true;
        IsAssignFormVisible = false;
        IsProgressFormVisible = false;
    }

    [RelayCommand]
    private void OpenAssignForm()
    {
        if (!CanAssignLearning)
            return;

        if (SelectedCourse is null)
        {
            StatusMessage = "Выберите курс";
            return;
        }

        ClearMessages();
        AssignmentDueDateText = DateTime.Today.AddMonths(1).ToString("dd.MM.yyyy");
        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();

        IsCreateCourseFormVisible = false;
        IsEditCourseFormVisible = false;
        IsAssignFormVisible = true;
        IsProgressFormVisible = false;

        if (EmployeeOptions.Count == 0)
            AssignmentFormMessage = "Нет доступных сотрудников для назначения";
    }

    [RelayCommand]
    private void OpenProgressForm()
    {
        if (SelectedAssignment is null)
        {
            StatusMessage = "Выберите назначение обучения";
            return;
        }

        ClearMessages();
        ProgressPercentText = FormatNumber(SelectedAssignment.Assignment.ProgressPercent);
        ProgressScoreText = SelectedAssignment.Assignment.Score is null
            ? string.Empty
            : FormatNumber(SelectedAssignment.Assignment.Score.Value);

        IsCreateCourseFormVisible = false;
        IsEditCourseFormVisible = false;
        IsAssignFormVisible = false;
        IsProgressFormVisible = true;
    }

    [RelayCommand]
    private void CloseForms()
    {
        IsCreateCourseFormVisible = false;
        IsEditCourseFormVisible = false;
        IsAssignFormVisible = false;
        IsProgressFormVisible = false;
        ClearMessages();
    }

    [RelayCommand]
    private async Task CreateCourseAsync()
    {
        if (!CanManageCourses)
            return;

        if (!TryParseDouble(NewCourseDurationText, out var durationHours))
        {
            CourseFormMessage = "Введите длительность числом";
            return;
        }

        var result = await _learningService.CreateCourseAsync(new CreateLearningCourseRequest
        {
            Title = NewCourseTitle,
            Description = NewCourseDescription,
            Format = SelectedNewFormat.Format,
            Provider = NewCourseProvider,
            DurationHours = durationHours
        });

        if (!result.Success)
        {
            CourseFormMessage = result.Message;
            return;
        }

        await RefreshAsync();
        SelectedCourse = result.Course is null
            ? SelectedCourse
            : Courses.FirstOrDefault(course => course.Course.Id == result.Course.Id);
        IsCreateCourseFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task SaveCourseAsync()
    {
        if (!CanManageCourses)
            return;

        if (SelectedCourse is null)
        {
            CourseFormMessage = "Выберите курс";
            return;
        }

        if (!TryParseDouble(EditCourseDurationText, out var durationHours))
        {
            CourseFormMessage = "Введите длительность числом";
            return;
        }

        var selectedCourseId = SelectedCourse.Course.Id;
        var result = await _learningService.UpdateCourseAsync(new UpdateLearningCourseRequest
        {
            CourseId = selectedCourseId,
            Title = EditCourseTitle,
            Description = EditCourseDescription,
            Format = SelectedEditFormat.Format,
            Provider = EditCourseProvider,
            DurationHours = durationHours,
            Status = SelectedEditCourseStatus.Status
        });

        if (!result.Success)
        {
            CourseFormMessage = result.Message;
            return;
        }

        await RefreshAsync();
        SelectedCourse = Courses.FirstOrDefault(course => course.Course.Id == selectedCourseId);
        IsEditCourseFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task AssignCourseAsync()
    {
        if (!CanAssignLearning)
            return;

        if (SelectedCourse is null)
        {
            AssignmentFormMessage = "Выберите курс";
            return;
        }

        if (SelectedEmployeeOption is null)
        {
            AssignmentFormMessage = "Выберите сотрудника";
            return;
        }

        DateTime? dueDate = null;

        if (!string.IsNullOrWhiteSpace(AssignmentDueDateText))
        {
            if (!TryParseDate(AssignmentDueDateText, out var parsedDueDate))
            {
                AssignmentFormMessage = "Введите срок в формате дд.мм.гггг";
                return;
            }

            dueDate = parsedDueDate;
        }

        var result = await _learningService.AssignCourseAsync(new AssignLearningCourseRequest
        {
            CourseId = SelectedCourse.Course.Id,
            EmployeeId = SelectedEmployeeOption.Employee.Id,
            DueDate = dueDate
        });

        if (!result.Success)
        {
            AssignmentFormMessage = result.Message;
            return;
        }

        await RefreshAsync();
        SelectedAssignment = result.Assignment is null
            ? SelectedAssignment
            : Assignments.FirstOrDefault(assignment => assignment.Assignment.Id == result.Assignment.Id);
        IsAssignFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task SaveProgressAsync()
    {
        if (SelectedAssignment is null)
        {
            ProgressFormMessage = "Выберите назначение обучения";
            return;
        }

        if (!TryParseDouble(ProgressPercentText, out var progressPercent))
        {
            ProgressFormMessage = "Введите прогресс числом";
            return;
        }

        double? score = null;

        if (!string.IsNullOrWhiteSpace(ProgressScoreText))
        {
            if (!TryParseDouble(ProgressScoreText, out var parsedScore))
            {
                ProgressFormMessage = "Введите оценку числом";
                return;
            }

            score = parsedScore;
        }

        var selectedAssignmentId = SelectedAssignment.Assignment.Id;
        var result = await _learningService.UpdateAssignmentProgressAsync(new UpdateLearningAssignmentProgressRequest
        {
            AssignmentId = selectedAssignmentId,
            ProgressPercent = progressPercent,
            Score = score
        });

        if (!result.Success)
        {
            ProgressFormMessage = result.Message;
            return;
        }

        await RefreshAsync();
        SelectedAssignment = Assignments.FirstOrDefault(assignment => assignment.Assignment.Id == selectedAssignmentId);
        IsProgressFormVisible = false;
        StatusMessage = result.Message;
    }

    [RelayCommand]
    private async Task CancelSelectedAssignmentAsync()
    {
        if (!CanAssignLearning)
            return;

        if (SelectedAssignment is null)
        {
            StatusMessage = "Выберите назначение обучения";
            return;
        }

        var selectedAssignmentId = SelectedAssignment.Assignment.Id;
        var result = await _learningService.CancelAssignmentAsync(selectedAssignmentId);
        StatusMessage = result.Message;

        if (!result.Success)
            return;

        await RefreshAsync();
        SelectedAssignment = Assignments.FirstOrDefault(assignment => assignment.Assignment.Id == selectedAssignmentId);
        StatusMessage = result.Message;
    }

    private async Task LoadEmployeesAsync()
    {
        IReadOnlyList<EmployeeProfile> employees = [];

        if (CanAssignLearning)
        {
            var employeesResult = await _employeeService.GetAllEmployeesAsync();

            if (employeesResult.Employees is not null)
                employees = employeesResult.Employees;

            if (!CanManageCourses)
            {
                var currentEmployeeResult = await _employeeService.GetCurrentEmployeeProfileAsync();

                if (currentEmployeeResult.Employee is not null)
                {
                    var currentEmployee = currentEmployeeResult.Employee;
                    employees = employees
                        .Where(employee => employee.ManagerEmployeeId == currentEmployee.Id)
                        .ToList();
                }
            }
        }
        else
        {
            var currentEmployeeResult = await _employeeService.GetCurrentEmployeeProfileAsync();

            if (currentEmployeeResult.Employee is not null)
                employees = [currentEmployeeResult.Employee];
        }

        employees = employees
            .Where(employee => employee.Status != EmployeeStatus.Dismissed)
            .ToList();

        _employeesById = employees.ToDictionary(employee => employee.Id);

        EmployeeOptions = new ObservableCollection<LearningEmployeeOption>(
            employees
                .OrderBy(employee => employee.FullName)
                .Select(employee => new LearningEmployeeOption(employee)));

        SelectedEmployeeOption = EmployeeOptions.FirstOrDefault();
    }

    private void HideFormsAfterSelectionChanged()
    {
        IsCreateCourseFormVisible = false;
        IsEditCourseFormVisible = false;
        IsAssignFormVisible = false;
        IsProgressFormVisible = false;
        CourseFormMessage = string.Empty;
        AssignmentFormMessage = string.Empty;
        ProgressFormMessage = string.Empty;
    }

    private void ClearCreateCourseForm()
    {
        NewCourseTitle = string.Empty;
        NewCourseDescription = string.Empty;
        NewCourseProvider = string.Empty;
        NewCourseDurationText = string.Empty;
        SelectedNewFormat = FormatOptions[0];
        CourseFormMessage = string.Empty;
    }

    private void ClearMessages()
    {
        StatusMessage = string.Empty;
        CourseFormMessage = string.Empty;
        AssignmentFormMessage = string.Empty;
        ProgressFormMessage = string.Empty;
    }

    private void UpdateSummary()
    {
        TotalCourseCount = Courses.Count;
        ActiveCourseCount = Courses.Count(course => course.Course.Status == LearningCourseStatus.Active);
        ActiveAssignmentCount = Assignments.Count(assignment =>
            assignment.Assignment.Status is LearningAssignmentStatus.Assigned or LearningAssignmentStatus.InProgress);
        CompletedAssignmentCount = Assignments.Count(assignment =>
            assignment.Assignment.Status == LearningAssignmentStatus.Completed);
    }

    private static bool TryParseDouble(string value, out double number)
    {
        value = value.Trim().Replace(',', '.');

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out number);
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        return DateTime.TryParseExact(
                value.Trim(),
                ["dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd"],
                CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.None,
                out date)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    partial void OnSelectedCourseChanged(LearningCourseRowViewModel? value)
    {
        if (IsCreateCourseFormVisible || IsEditCourseFormVisible || IsAssignFormVisible)
            HideFormsAfterSelectionChanged();

        if (value is not null)
        {
            IsCourseDetailsActive = true;
            SelectedAssignment = null;
        }

        OnPropertyChanged(nameof(HasSelectedCourse));
        OnPropertyChanged(nameof(HasNoSelectedCourse));
        OnPropertyChanged(nameof(CanShowCourseActions));
        OnPropertyChanged(nameof(CanShowAssignAction));
        OnPropertyChanged(nameof(CanShowCourseDetails));
        OnPropertyChanged(nameof(CanShowCoursePrompt));
        OnPropertyChanged(nameof(SelectedCourseCaption));
    }

    partial void OnSelectedAssignmentChanged(LearningAssignmentRowViewModel? value)
    {
        if (IsProgressFormVisible)
            HideFormsAfterSelectionChanged();

        if (value is not null)
        {
            IsCourseDetailsActive = false;
            SelectedCourse = null;
        }

        OnPropertyChanged(nameof(HasSelectedAssignment));
        OnPropertyChanged(nameof(HasNoSelectedAssignment));
        OnPropertyChanged(nameof(CanShowAssignmentActions));
        OnPropertyChanged(nameof(CanShowAssignmentDetails));
        OnPropertyChanged(nameof(CanShowAssignmentPrompt));
        OnPropertyChanged(nameof(SelectedAssignmentCaption));
    }

    partial void OnIsCreateCourseFormVisibleChanged(bool value)
    {
        NotifyDetailsVisibilityChanged();
    }

    partial void OnIsEditCourseFormVisibleChanged(bool value)
    {
        NotifyDetailsVisibilityChanged();
    }

    partial void OnIsAssignFormVisibleChanged(bool value)
    {
        NotifyDetailsVisibilityChanged();
    }

    partial void OnIsProgressFormVisibleChanged(bool value)
    {
        NotifyDetailsVisibilityChanged();
    }

    partial void OnIsCourseDetailsActiveChanged(bool value)
    {
        NotifyDetailsVisibilityChanged();
    }

    private void NotifyDetailsVisibilityChanged()
    {
        OnPropertyChanged(nameof(CanShowCourseDetails));
        OnPropertyChanged(nameof(CanShowAssignmentDetails));
        OnPropertyChanged(nameof(CanShowCoursePrompt));
        OnPropertyChanged(nameof(CanShowAssignmentPrompt));
    }

    partial void OnStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    partial void OnCourseFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasCourseFormMessage));
    }

    partial void OnAssignmentFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasAssignmentFormMessage));
    }

    partial void OnProgressFormMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasProgressFormMessage));
    }
}

public sealed class LearningCourseRowViewModel
{
    public LearningCourse Course { get; }

    public string Title => Course.Title;

    public string Description => string.IsNullOrWhiteSpace(Course.Description)
        ? "Без описания"
        : Course.Description;

    public string Provider => Course.Provider;

    public string DurationText => $"{Course.DurationHours:0.##} ч";

    public string FormatTitle => DisplayNames.ForLearningFormat(Course.Format);

    public string StatusTitle => DisplayNames.ForLearningCourseStatus(Course.Status);

    public string CreatedAtText => Course.CreatedAt.ToString("dd.MM.yyyy HH:mm");

    public string UpdatedAtText => Course.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public LearningCourseRowViewModel(LearningCourse course)
    {
        Course = course;
    }
}

public sealed class LearningAssignmentRowViewModel
{
    public LearningAssignment Assignment { get; }

    private readonly LearningCourse? _course;

    private readonly EmployeeProfile? _employee;

    public string CourseTitle => _course?.Title ?? "Курс не найден";

    public string EmployeeTitle => _employee is null
        ? "Сотрудник не найден"
        : $"{_employee.FullName} · {_employee.PersonnelNumber}";

    public string DueDateText => Assignment.DueDate?.ToString("dd.MM.yyyy") ?? "Без срока";

    public string AssignedAtText => Assignment.AssignedAt.ToString("dd.MM.yyyy");

    public string StartedAtText => Assignment.StartedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string CompletedAtText => Assignment.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string UpdatedAtText => Assignment.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";

    public string ProgressText => $"{Assignment.ProgressPercent:0.##}%";

    public string ScoreText => Assignment.Score is null ? "-" : $"{Assignment.Score:0.##}";

    public string StatusTitle => DisplayNames.ForLearningAssignmentStatus(Assignment.Status);

    public LearningAssignmentRowViewModel(
        LearningAssignment assignment,
        LearningCourse? course,
        EmployeeProfile? employee)
    {
        Assignment = assignment;
        _course = course;
        _employee = employee;
    }
}

public sealed class LearningEmployeeOption
{
    public EmployeeProfile Employee { get; }

    public string Title => $"{Employee.FullName} ({Employee.PersonnelNumber})";

    public LearningEmployeeOption(EmployeeProfile employee)
    {
        Employee = employee;
    }
}

public sealed class LearningFormatOption
{
    public static IReadOnlyList<LearningFormatOption> All { get; } =
    [
        new LearningFormatOption(LearningFormat.Online),
        new LearningFormatOption(LearningFormat.Offline),
        new LearningFormatOption(LearningFormat.Mixed)
    ];

    public LearningFormat Format { get; }

    public string Title => DisplayNames.ForLearningFormat(Format);

    private LearningFormatOption(LearningFormat format)
    {
        Format = format;
    }
}

public sealed class LearningCourseStatusOption
{
    public static IReadOnlyList<LearningCourseStatusOption> All { get; } =
    [
        new LearningCourseStatusOption(LearningCourseStatus.Draft),
        new LearningCourseStatusOption(LearningCourseStatus.Active),
        new LearningCourseStatusOption(LearningCourseStatus.Archived)
    ];

    public LearningCourseStatus Status { get; }

    public string Title => DisplayNames.ForLearningCourseStatus(Status);

    private LearningCourseStatusOption(LearningCourseStatus status)
    {
        Status = status;
    }
}
