using MBappe.Models;
using System.Collections.Generic;

namespace MBappe.Common;

public class MotivationOperationResult
{
    public bool Success { get; }

    public string Message { get; }

    public MotivationProgram? Program { get; }

    public IReadOnlyList<MotivationProgram>? Programs { get; }

    public MotivationBonus? Bonus { get; }

    public IReadOnlyList<MotivationBonus>? Bonuses { get; }

    private MotivationOperationResult(
        bool success,
        string message,
        MotivationProgram? program = null,
        IReadOnlyList<MotivationProgram>? programs = null,
        MotivationBonus? bonus = null,
        IReadOnlyList<MotivationBonus>? bonuses = null)
    {
        Success = success;
        Message = message;
        Program = program;
        Programs = programs;
        Bonus = bonus;
        Bonuses = bonuses;
    }

    public static MotivationOperationResult Ok(string message)
    {
        return new MotivationOperationResult(true, message);
    }

    public static MotivationOperationResult Ok(MotivationProgram program, string message)
    {
        return new MotivationOperationResult(true, message, program: program);
    }

    public static MotivationOperationResult Ok(IReadOnlyList<MotivationProgram> programs, string message)
    {
        return new MotivationOperationResult(true, message, programs: programs);
    }

    public static MotivationOperationResult Ok(MotivationBonus bonus, string message)
    {
        return new MotivationOperationResult(true, message, bonus: bonus);
    }

    public static MotivationOperationResult Ok(IReadOnlyList<MotivationBonus> bonuses, string message)
    {
        return new MotivationOperationResult(true, message, bonuses: bonuses);
    }

    public static MotivationOperationResult Fail(string message)
    {
        return new MotivationOperationResult(false, message);
    }
}