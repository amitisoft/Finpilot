namespace FinPilot.Application.Interfaces.Reports;

public interface IReportExportService
{
    Task<(byte[] Content, string FileName)> GenerateFinancialReportPdfAsync(Guid userId, CancellationToken cancellationToken = default);
}