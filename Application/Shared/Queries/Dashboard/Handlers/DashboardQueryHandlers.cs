using Domain.Common.Dashboard;
using Domain.Interfaces.Repositories;
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
}
