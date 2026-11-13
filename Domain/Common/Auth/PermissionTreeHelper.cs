using Domain.Entities;

namespace Domain.Common.Auth
{
    /// <summary>
    /// Utilidades para resolver ancestros activos y normalizar el árbol de permisos.
    /// </summary>
    public static class PermissionTreeHelper
    {
        /// <summary>
        /// Resuelve los permisos activos solicitados junto con todos sus ancestros activos hasta la raíz.
        /// </summary>
        public static List<Permission> ResolveActiveWithAncestors(
            IEnumerable<Permission> activePermissions,
            IEnumerable<string> requestedCodes)
        {
            ArgumentNullException.ThrowIfNull(activePermissions);
            ArgumentNullException.ThrowIfNull(requestedCodes);

            var byCode = new Dictionary<string, Permission>(StringComparer.Ordinal);
            foreach (var perm in activePermissions)
            {
                if (perm.StatusRegister && !string.IsNullOrWhiteSpace(perm.Code))
                {
                    byCode.TryAdd(perm.Code, perm);
                }
            }

            var resolved = new List<Permission>();
            var visitedCodes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var requestedCode in requestedCodes)
            {
                var currentCode = requestedCode;
                while (!string.IsNullOrWhiteSpace(currentCode) &&
                       visitedCodes.Add(currentCode) &&
                       byCode.TryGetValue(currentCode, out var permission))
                {
                    resolved.Add(permission);
                    currentCode = permission.ParentCode;
                }
            }

            return resolved;
        }

        /// <summary>
        /// Resuelve los permisos activos identificados por ID junto con todos sus ancestros activos hasta la raíz.
        /// </summary>
        public static List<Permission> ResolveActiveWithAncestorsByIds(
            IEnumerable<Permission> activePermissions,
            IEnumerable<int> requestedPermissionIds)
        {
            ArgumentNullException.ThrowIfNull(activePermissions);
            ArgumentNullException.ThrowIfNull(requestedPermissionIds);

            var activeList = activePermissions
                .Where(p => p.StatusRegister && !string.IsNullOrWhiteSpace(p.Code))
                .ToList();

            var codesById = new Dictionary<int, string>();
            foreach (var perm in activeList)
            {
                codesById.TryAdd(perm.Id, perm.Code);
            }

            var requestedCodes = requestedPermissionIds
                .Select(id => codesById.TryGetValue(id, out var code) ? code : null)
                .Where(code => code != null)
                .Cast<string>();

            return ResolveActiveWithAncestors(activeList, requestedCodes);
        }

        /// <summary>
        /// Poda contenedores sin hijos activos de forma ascendente y completa los ancestros activos de las hojas conservadas.
        /// </summary>
        public static List<PermissionInfo> NormalizeActiveLoginPermissions(
            IEnumerable<Permission> activeCatalog,
            IEnumerable<int> candidatePermissionIds)
        {
            ArgumentNullException.ThrowIfNull(activeCatalog);
            ArgumentNullException.ThrowIfNull(candidatePermissionIds);

            var activeList = activeCatalog
                .Where(p => p.StatusRegister && !string.IsNullOrWhiteSpace(p.Code))
                .ToList();

            if (activeList.Count == 0)
            {
                return new List<PermissionInfo>();
            }

            var byCode = new Dictionary<string, Permission>(StringComparer.Ordinal);
            var byId = new Dictionary<int, Permission>();
            foreach (var perm in activeList)
            {
                byCode.TryAdd(perm.Code, perm);
                byId.TryAdd(perm.Id, perm);
            }

            var childrenOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var perm in activeList)
            {
                if (!string.IsNullOrWhiteSpace(perm.ParentCode) && byCode.ContainsKey(perm.ParentCode))
                {
                    if (!childrenOf.TryGetValue(perm.ParentCode, out var children))
                    {
                        children = new List<string>();
                        childrenOf[perm.ParentCode] = children;
                    }
                    children.Add(perm.Code);
                }
            }

            var activeCodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in candidatePermissionIds)
            {
                if (byId.TryGetValue(id, out var perm))
                {
                    activeCodes.Add(perm.Code);
                }
            }

            bool changed;
            do
            {
                changed = false;
                foreach (var code in activeCodes.ToList())
                {
                    if (childrenOf.TryGetValue(code, out var catalogChildren) &&
                        catalogChildren.Count > 0 &&
                        !catalogChildren.Any(activeCodes.Contains))
                    {
                        activeCodes.Remove(code);
                        changed = true;
                    }
                }
            } while (changed);

            if (activeCodes.Count == 0)
            {
                return new List<PermissionInfo>();
            }

            var withAncestors = ResolveActiveWithAncestors(activeList, activeCodes);
            var finalIds = withAncestors.Select(p => p.Id).ToHashSet();

            return activeList
                .Where(p => finalIds.Contains(p.Id))
                .Select(p => new PermissionInfo { Code = p.Code, ParentCode = p.ParentCode })
                .ToList();
        }
    }
}
