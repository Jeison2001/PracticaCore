using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Auth;
using System.Linq.Expressions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<UserRole>> GetUserRolesWithUserDetailsAsync(
            string? roleCode,
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool isDescending,
            Dictionary<string, string>? filters,
            CancellationToken cancellationToken)
        {
            // Crear la consulta base con las relaciones incluidas
            IQueryable<UserRole> query = _context.Set<UserRole>()
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .AsNoTracking() // Mejora el rendimiento para consultas de solo lectura
                .Where(ur => ur.StatusRegister);

            // Filtrar por código de rol si se proporciona
            if (!string.IsNullOrEmpty(roleCode))
            {
                string roleCodeLower = roleCode.ToLower();
                query = query.Where(ur => ur.Role.Code.ToLower() == roleCodeLower);
            }

            // Aplicar filtros adicionales
            if (filters != null && filters.Any())
            {
                // Construir expresión LINQ a partir de los filtros adicionales
                var additionalFilter = FilterBuilder.BuildFilter<UserRole, int>(filters);
                if (additionalFilter != null)
                {
                    query = query.Where(additionalFilter);
                }
            }

            // Contamos el total de registros para la paginación (antes de aplicar ordenación y paginación)
            var totalCount = await query.CountAsync(cancellationToken);

            // Aplicar ordenación
            IQueryable<UserRole> orderedQuery;
            if (!string.IsNullOrEmpty(sortBy))
            {
                // Mapeamos algunos campos comunes que podrían venir en el sortBy para usar propiedades de navegación
                sortBy = sortBy.ToLower() switch
                {
                    "rolecode" => "Role.Code",
                    "rolename" => "Role.Name",
                    "email" => "User.Email",
                    "firstname" => "User.FirstName",
                    "lastname" => "User.LastName",
                    "identification" => "User.Identification",
                    _ => sortBy
                };

                try
                {
                    // Intentamos ordenar dinámicamente
                    if (sortBy.Contains("."))
                    {
                        // Si es una propiedad de navegación (ej: "Role.Name")
                        var parts = sortBy.Split('.');
                        if (parts.Length == 2)
                        {
                            var navigation = parts[0];
                            var property = parts[1];

                            orderedQuery = navigation.ToLower() switch
                            {
                                "role" => isDescending 
                                    ? query.OrderByDescending(ur => EF.Property<object>(ur.Role, property))
                                    : query.OrderBy(ur => EF.Property<object>(ur.Role, property)),
                                "user" => isDescending 
                                    ? query.OrderByDescending(ur => EF.Property<object>(ur.User, property))
                                    : query.OrderBy(ur => EF.Property<object>(ur.User, property)),
                                _ => isDescending
                                    ? query.OrderByDescending(ur => ur.Id)
                                    : query.OrderBy(ur => ur.Id)
                            };
                        }
                        else
                        {
                            // Por defecto, si no podemos procesar
                            orderedQuery = isDescending
                                ? query.OrderByDescending(ur => ur.Id)
                                : query.OrderBy(ur => ur.Id);
                        }
                    }
                    else
                    {
                        // Ordenación por una propiedad directa
                        orderedQuery = isDescending
                            ? query.OrderByDescending(e => EF.Property<object>(e, sortBy))
                            : query.OrderBy(e => EF.Property<object>(e, sortBy));
                    }
                }
                catch
                {
                    // Si hay algún error en la ordenación, usamos la ordenación por defecto
                    orderedQuery = isDescending
                        ? query.OrderByDescending(ur => ur.Id)
                        : query.OrderBy(ur => ur.Id);
                }
            }
            else
            {
                // Ordenación por defecto
                orderedQuery = query.OrderBy(ur => ur.Id);
            }

            // Aplicar paginación
            var pagedItems = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Devolver el resultado paginado con los datos relacionados ya incluidos
            return new PaginatedResult<UserRole>
            {
                Items = pagedItems,
                TotalRecords = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}