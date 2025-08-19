namespace Itarix.Api.Models
{
    public class User
    {
        public int UserId { get; set; }
        public int PersonId { get; set; }         // Link to Person table (foreign key)
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string EmailVerificationToken { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        // public string Email { get; set; } // REMOVE this if now using Person

        public bool IsDeleted { get; set; }  // Add this property to track soft deletion

    }

    public class RefreshRequestDto
    {
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; }
    }

    public class ResetPasswordDto
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
