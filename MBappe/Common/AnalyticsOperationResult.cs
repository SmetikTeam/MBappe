namespace MBappe.Common;

public sealed class AnalyticsOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public AnalyticsReport? Report { get; }

    private AnalyticsOperationResult(
        bool success,
        string message,
        AnalyticsReport? report = null)
    {
        Success = success;
        Message = message;
        Report = report;
    }

    public static AnalyticsOperationResult Ok(AnalyticsReport report, string message)
    {
        return new AnalyticsOperationResult(true, message, report);
    }

    public static AnalyticsOperationResult Fail(string message)
    {
        return new AnalyticsOperationResult(false, message);
    }
}
