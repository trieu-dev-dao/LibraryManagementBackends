namespace LibraryManagement.DTOs;

public record DashboardMetricsDto(long? TotalOwnedCopies, int CurrentlyOverdueLoans,
    int PendingReconciliationCount);
