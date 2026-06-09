using Domain.Common.Dashboard;
using MediatR;

namespace Application.Shared.Queries.Dashboard
{
    public record GetDashboardSummaryQuery(DashboardFilter Filter) : IRequest<DashboardSummary>;

    public record GetDashboardFilterOptionsQuery : IRequest<DashboardFilterOptions>;

    public record GetDashboardExportQuery(DashboardFilter Filter) : IRequest<List<DashboardExportRow>>;

    public record GetModalityBreakdownQuery(DashboardFilter Filter) : IRequest<ModalityBreakdown?>;
}
