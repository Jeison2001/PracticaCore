using Domain.Common.Dashboard;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using MediatR;

namespace Application.Shared.Queries.Dashboard.Handlers
{
    public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummary>
    {
        private readonly IDashboardRepository _repository;
        public GetDashboardSummaryHandler(IDashboardRepository repository) => _repository = repository;

        public Task<DashboardSummary> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
            => _repository.GetSummaryAsync(request.Filter, cancellationToken);
    }

    public class GetDashboardFilterOptionsHandler : IRequestHandler<GetDashboardFilterOptionsQuery, DashboardFilterOptions>
    {
        private readonly IDashboardRepository _repository;
        public GetDashboardFilterOptionsHandler(IDashboardRepository repository) => _repository = repository;

        public Task<DashboardFilterOptions> Handle(GetDashboardFilterOptionsQuery request, CancellationToken cancellationToken)
            => _repository.GetFilterOptionsAsync(cancellationToken);
    }

    public class GetDashboardExportHandler : IRequestHandler<GetDashboardExportQuery, List<DashboardExportRow>>
    {
        private readonly IDashboardRepository _repository;
        public GetDashboardExportHandler(IDashboardRepository repository) => _repository = repository;

        public Task<List<DashboardExportRow>> Handle(GetDashboardExportQuery request, CancellationToken cancellationToken)
            => _repository.GetExportRowsAsync(request.Filter, cancellationToken);
    }

    public class GetModalityBreakdownHandler : IRequestHandler<GetModalityBreakdownQuery, ModalityBreakdown?>
    {
        private readonly IDashboardRepository _repository;
        public GetModalityBreakdownHandler(IDashboardRepository repository) => _repository = repository;

        public Task<ModalityBreakdown?> Handle(GetModalityBreakdownQuery request, CancellationToken cancellationToken)
            => _repository.GetModalityBreakdownAsync(request.Filter, cancellationToken);
    }

    public class GetDashboardAlertsHandler : IRequestHandler<GetDashboardAlertsQuery, DashboardOperationalAlerts>
    {
        private readonly IDashboardRepository _repository;
        public GetDashboardAlertsHandler(IDashboardRepository repository) => _repository = repository;

        public Task<DashboardOperationalAlerts> Handle(GetDashboardAlertsQuery request, CancellationToken cancellationToken)
            => _repository.GetOperationalAlertsAsync(request.Filter, cancellationToken);
    }

    public class GetDashboardRecordsHandler : IRequestHandler<GetDashboardRecordsQuery, DashboardRecordsPage>
    {
        private readonly IDashboardRepository _repository;
        public GetDashboardRecordsHandler(IDashboardRepository repository) => _repository = repository;

        public Task<DashboardRecordsPage> Handle(GetDashboardRecordsQuery request, CancellationToken cancellationToken)
            => _repository.GetPagedRecordsAsync(request.Filter, cancellationToken);
    }

    public class GetDashboardExcelHandler : IRequestHandler<GetDashboardExcelQuery, byte[]>
    {
        private readonly IDashboardRepository _repository;
        private readonly IDashboardExcelExporter _excelExporter;

        public GetDashboardExcelHandler(IDashboardRepository repository, IDashboardExcelExporter excelExporter)
        {
            _repository = repository;
            _excelExporter = excelExporter;
        }

        public async Task<byte[]> Handle(GetDashboardExcelQuery request, CancellationToken cancellationToken)
        {
            var filter = request.Filter ?? new DashboardFilter();

            // Ejecutar secuencialmente sobre el mismo AppDbContext scoped (EF Core no es thread-safe).
            var summary = await _repository.GetSummaryAsync(filter, cancellationToken);
            ModalityBreakdown? breakdown = filter.ModalityCodes is { Count: 1 }
                ? await _repository.GetModalityBreakdownAsync(filter, cancellationToken)
                : null;
            var alerts = await _repository.GetOperationalAlertsAsync(filter, cancellationToken);
            var rows = await _repository.GetExportRowsAsync(filter, cancellationToken);
            var options = await _repository.GetFilterOptionsAsync(cancellationToken);

            return _excelExporter.GenerateExcel(filter, summary, breakdown, alerts, rows, options);
        }
    }
}
