using System;

namespace MBappe.ViewModels.Shell;

public sealed class NavigationItemViewModel : ViewModelBase
{
    private readonly Func<ViewModelBase> _createViewModel;

    public string Title { get; }

    public string Code { get; }

    public string Description { get; }

    public NavigationItemViewModel(
        string title,
        string code,
        string description,
        Func<ViewModelBase> createViewModel)
    {
        Title = title;
        Code = code;
        Description = description;
        _createViewModel = createViewModel;
    }

    public ViewModelBase CreateViewModel()
    {
        return _createViewModel();
    }
}
