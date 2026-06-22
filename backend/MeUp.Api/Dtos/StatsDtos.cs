namespace MeUp.Api.Dtos;

public record CategoryStat(string Name, string? Color, string Type, decimal Amount);
public record DailyNet(DateOnly Date, decimal Income, decimal Expense);
public record FinanceStats(
    decimal TotalIncome,
    decimal TotalExpense,
    IReadOnlyList<CategoryStat> ByCategory,
    IReadOnlyList<DailyNet> Daily);

public record WeightPoint(DateOnly Date, decimal Weight);
public record HealthStats(
    decimal? AvgWeight,
    decimal? AvgSleep,
    int? AvgWater,
    int? AvgWorkout,
    int Days,
    IReadOnlyList<WeightPoint> WeightSeries);

public record WorkStats(
    int TasksTotal,
    int TasksDone,
    int GoalsCount,
    int GoalsAvgProgress,
    int HabitsTotal,
    int HabitChecks);

public record StatsDto(
    DateOnly From,
    DateOnly To,
    FinanceStats Finance,
    HealthStats Health,
    WorkStats Work);
