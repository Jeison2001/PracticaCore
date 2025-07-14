using Hangfire.Dashboard;

namespace Api.Middlewares
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // En desarrollo, permitir acceso sin autenticación
            // En producción, aquí deberías implementar tu lógica de autorización
            return true;
        }
    }
}
