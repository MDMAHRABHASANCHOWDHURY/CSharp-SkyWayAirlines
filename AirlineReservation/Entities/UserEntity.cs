using System;
namespace AirlineReservationDemo.Entities
{
    internal sealed class UserEntity
    {
        public bool MustChangePassword { get; set; }
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public int? PassId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin
        {
            get {
                return Role == "admin";
            }
        }
    }
    internal static class Session
    {
        public static UserEntity Current { get; private set; }
        public static bool IsSignedIn
        {
            get {
                return Current != null;
            }
        }
        public static void Start(UserEntity user)
        {
            Current = user;
        }
        public static void End()
        {
            Current = null;
        }
        public static void RequireAdmin()
        {
            if (!IsSignedIn || !Current.IsAdmin)
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
        public static void RequireUser(int id)
        {
            if (!IsSignedIn || (!Current.IsAdmin && Current.Id != id))
            throw new UnauthorizedAccessException("Please sign in to your own account.");
        }
    }
}
