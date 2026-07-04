namespace RpFlo.Application.DTOs;

public sealed record DashboardMetrics(
    int TotalRequests,
    int DraftCount,
    int PendingApprovalCount,
    int ApprovedCount,
    int RejectedCount,
    int PurchaseOrderCount,
    decimal TotalApprovedAmount,
    decimal AverageProcessingTimeHours,
    List<StatusCount> StatusBreakdown,
    List<DepartmentCount> DepartmentBreakdown,
    RoleMetrics? RoleMetrics = null);

public sealed record RoleMetrics(
    int MyActiveRequests,
    int MyPendingApproval,
    decimal MyApprovedAmount,
    decimal MyAvgProcessingHours,
    int PendingMyReview,
    int ApprovedThisMonth,
    int ReadyForPo,
    decimal TotalValuePending,
    decimal MonthlySpendApproved,
    List<StatusCount> MyStatusBreakdown,
    List<DepartmentCount> MyDepartmentBreakdown);

public sealed record StatusCount(string Status, int Count);
public sealed record DepartmentCount(string Department, int Count, decimal TotalAmount);
