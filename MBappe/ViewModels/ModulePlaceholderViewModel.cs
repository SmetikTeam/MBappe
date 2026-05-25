using System.Collections.ObjectModel;

namespace MBappe.ViewModels;

public sealed class ModulePlaceholderViewModel : ViewModelBase
{
    public string Title { get; }

    public string Subtitle { get; }

    public ObservableCollection<FeatureItemViewModel> Features { get; }

    public ModulePlaceholderViewModel(
        string title,
        string subtitle,
        ObservableCollection<FeatureItemViewModel> features)
    {
        Title = title;
        Subtitle = subtitle;
        Features = features;
    }

    public static ModulePlaceholderViewModel CreateKpiModule()
    {
        return new ModulePlaceholderViewModel(
            "KPI и эффективность",
            "Будущий раздел для оценки результатов сотрудников и команд.",
            [
                new FeatureItemViewModel("Личные показатели", "План, факт и процент выполнения по сотруднику"),
                new FeatureItemViewModel("Командные KPI", "Сводка по подразделениям и руководителям"),
                new FeatureItemViewModel("История оценки", "Архив результатов для прозрачной аналитики")
            ]);
    }

    public static ModulePlaceholderViewModel CreateLearningModule()
    {
        return new ModulePlaceholderViewModel(
            "Обучение и развитие",
            "Будущий раздел для курсов, назначений и контроля прогресса.",
            [
                new FeatureItemViewModel("Каталог курсов", "Программы обучения с описанием и сроками"),
                new FeatureItemViewModel("Назначения", "Курсы для сотрудников, групп и подразделений"),
                new FeatureItemViewModel("Результаты", "Факт прохождения, тестирование и прогресс")
            ]);
    }

    public static ModulePlaceholderViewModel CreateMotivationModule()
    {
        return new ModulePlaceholderViewModel(
            "Мотивация и бонусы",
            "Будущий раздел для прозрачного учета премий и нематериальной мотивации.",
            [
                new FeatureItemViewModel("Бонусные правила", "Условия начислений и согласований"),
                new FeatureItemViewModel("История премий", "Основания, суммы и статусы решений"),
                new FeatureItemViewModel("Вовлеченность", "Персональные достижения и обратная связь")
            ]);
    }

    public static ModulePlaceholderViewModel CreateAnalyticsModule()
    {
        return new ModulePlaceholderViewModel(
            "Аналитика и отчеты",
            "Будущий раздел для управленческих отчетов по персоналу.",
            [
                new FeatureItemViewModel("Кадровая сводка", "Состав персонала и активность учетных записей"),
                new FeatureItemViewModel("Обучение и KPI", "Сравнение развития и эффективности"),
                new FeatureItemViewModel("Экспорт отчетов", "Подготовка материалов для руководства")
            ]);
    }
}

public sealed class FeatureItemViewModel
{
    public string Title { get; }

    public string Caption { get; }

    public FeatureItemViewModel(string title, string caption)
    {
        Title = title;
        Caption = caption;
    }
}
