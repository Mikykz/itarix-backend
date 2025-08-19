using Itarix.Api.Data;
using Itarix.Api.Models;
using System;
using BCrypt.Net;

namespace Itarix.Api.Business
{
    public class UserService
    {
        private readonly UserRepository _userRepo;

        public UserService(UserRepository userRepo)
        {
            _userRepo = userRepo;
        }
        public void UpdateUser(User user)
        {
            _userRepo.UpdateUser(user);
        }
        public User GetUserByEmailToken(string token)
        {
            return _userRepo.GetUserByEmailToken(token);
        }
        public User GetUserByRefreshToken(string refreshToken)
        {
            return _userRepo.GetUserByRefreshToken(refreshToken);
        }

        public Person GetPersonById(int personId)
        {
            return _userRepo.GetPersonById(personId);
        }
        public User GetUserById(int userId)
        {
            // Your logic here to fetch user by ID from the database
            return _userRepo.GetUserById(userId);
        }
        public User GetUserByEmail(string email) => _userRepo.GetUserByEmail(email);

        public User GetUserByPasswordResetToken(string token) => _userRepo.GetUserByPasswordResetToken(token);

        // Registration logic
        public int RegisterUser(RegisterDto dto)
        {
            // Check username, excluding soft-deleted users
            var existing = _userRepo.GetUserByUsername(dto.Username);
            if (existing != null && !existing.IsDeleted)
                throw new Exception("Username already exists");

            // Check email and decide whether to reuse Person or create new
            var existingEmailPerson = _userRepo.GetPersonByEmail(dto.Email);
            int personId;

            if (existingEmailPerson != null)
            {
                var userByEmail = _userRepo.GetUserByPersonId(existingEmailPerson.PersonId);
                if (userByEmail != null && !userByEmail.IsDeleted)
                    throw new Exception("Email already exists");

                // No active user linked: reuse existing person
                personId = existingEmailPerson.PersonId;
            }
            else
            {
                // No person with that email exists: create new
                var person = new Person
                {
                    Name = dto.Username,   // Or real name if you have it
                    Email = dto.Email,
                    // Phone = ... (optional)
                };
                personId = _userRepo.AddPerson(person);
            }

            // Hash password
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create user linked to person
            var user = new User
            {
                PersonId = personId,
                Username = dto.Username,
                PasswordHash = hash,
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                RefreshToken = null,
                RefreshTokenExpiry = null,
                IsEmailConfirmed = false,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                IsDeleted = false
            };

            // Save user to the database
            return _userRepo.AddUser(user);
        }




        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        // Login logic (used by API controller)
        public User Login(string username, string password)
        {
            var user = _userRepo.GetUserByUsername(username);
            if (user == null || user.IsDeleted)
                return null; // user doesn't exist or is deleted

            if (!user.IsEmailConfirmed)
                throw new Exception("Please verify your email before logging in.");

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                throw new Exception("Account locked. Try again later.");

            if (!VerifyPassword(password, user.PasswordHash))
            {
                user.FailedLoginAttempts += 1;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10); // Lock for 10 minutes
                    user.FailedLoginAttempts = 0; // Reset after lock
                }
                _userRepo.UpdateUser(user);
                return null;
            }

            // Success: reset
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            _userRepo.UpdateUser(user);

            return user;
        }


        public void SoftDeleteUser(int userId)
        {
            var user = _userRepo.GetUserById(userId);
            if (user == null) throw new Exception("User not found");

            user.IsDeleted = true;
            _userRepo.UpdateUser(user);
        }


        public string ForgotPassword(string email)
        {
            var person = _userRepo.GetPersonByEmail(email);
            if (person == null) throw new Exception("No user with this email.");

            var user = _userRepo.GetUserByPersonId(person.PersonId);
            if (user == null) throw new Exception("No user account for this person.");

            var token = Guid.NewGuid().ToString();
            var expiry = DateTime.UtcNow.AddMinutes(15);
            _userRepo.SetPasswordResetToken(user.UserId, token, expiry);
            return token;
        }


        // For custom checks elsewhere (optional)
        public User ValidateUser(string username, string password)
        {
            return Login(username, password);
        }
        public string GetEmailByUser(User user)
        {
            var person = _userRepo.GetPersonById(user.PersonId);
            return person?.Email ?? string.Empty;
        }
    }
}
