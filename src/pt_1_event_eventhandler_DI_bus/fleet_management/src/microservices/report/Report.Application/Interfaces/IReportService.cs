using Report.Application.Models;

namespace Report.Application.Interfaces;

public interface IReportService
{
    void ReportOcurrence(ReportOccurrenceRequest report);
}
