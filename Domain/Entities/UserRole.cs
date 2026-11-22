using Domain.Events;

namespace Domain.Entities
{
    public class UserRole : BaseEntity<int>
    {
        private int _idUser;
        public int IdUser
        {
            get => _idUser;
            set
            {
                _idUser = value;
                TryAddEvent();
            }
        }

        private int _idRole;
        public int IdRole
        {
            get => _idRole;
            set
            {
                _idRole = value;
                TryAddEvent();
            }
        }

        private void TryAddEvent()
        {
            if (_idUser != 0 && _idRole != 0)
            {
                ClearDomainEvents();
                var triggeredBy = IdUserUpdatedAt ?? IdUserCreatedAt;
                AddDomainEvent(new UserRoleAssignedEvent(_idUser, _idRole, triggeredBy is > 0 ? triggeredBy : null));
            }
        }

        // Relaciones
        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
    }
}