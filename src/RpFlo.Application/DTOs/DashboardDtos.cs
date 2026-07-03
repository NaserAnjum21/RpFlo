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
    List<DepartmentCount> DepartmentBreakdown);

public sealed record StatusCount(string Status, int Count);
public sealed record DepartmentCount(string Department, int Count, decimal TotalAmount);
