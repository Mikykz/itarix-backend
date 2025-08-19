using System;
using System.Data;
using Microsoft.Data.SqlClient;

using Itarix.Api.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Itarix.Api.Data
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
          
        }

        // Person table
        public int AddPerson(Person person)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                INSERT INTO Person (Name, Email, Phone)
                VALUES (@Name, @Email, @Phone);
                SELECT SCOPE_IDENTITY();
            ", conn))
            {
                cmd.Parameters.AddWithValue("@Name", person.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", person.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", person.Phone ?? (object)DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public Person GetPersonById(int personId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Person WHERE PersonId = @PersonId", conn))
            {
                cmd.Parameters.AddWithValue("@PersonId", personId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Person
                        {
                            PersonId = (int)reader["PersonId"],
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }



        public Person GetPersonByEmail(string email)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Person WHERE Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Person
                        {
                            PersonId = (int)reader["PersonId"],
                            Name = reader["Name"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }

        public void SoftDeleteUser(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UPDATE Users SET IsDeleted = 1 WHERE UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public User GetUserByPersonId(int personId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Users WHERE PersonId = @PersonId AND IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@PersonId", personId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader);
                }
            }
            return null;
        }


        // Add User (for registration)
        public int AddUser(User user)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                INSERT INTO Users (PersonId, Username, PasswordHash, Role, CreatedAt, IsEmailConfirmed, EmailVerificationToken, IsDeleted)
VALUES (@PersonId, @Username, @PasswordHash, @Role, @CreatedAt, @IsEmailConfirmed, @EmailVerificationToken, @IsDeleted);

                SELECT SCOPE_IDENTITY();
            ", conn))
            {
                cmd.Parameters.AddWithValue("@PersonId", user.PersonId);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                cmd.Parameters.AddWithValue("@Role", user.Role ?? "user");
                cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
                cmd.Parameters.AddWithValue("@IsEmailConfirmed", user.IsEmailConfirmed ? 1 : 0);
                cmd.Parameters.AddWithValue("@EmailVerificationToken", (object)user.EmailVerificationToken ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsDeleted", user.IsDeleted);


                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Get User by Username (LEFT JOIN for email, name, phone)
        public User GetUserByUsername(string username)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
        SELECT U.*, P.Email, P.Name, P.Phone
        FROM Users U
        LEFT JOIN Person P ON U.PersonId = P.PersonId
        WHERE U.Username = @Username AND U.IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader, joinedPerson: true);
                }
            }
            return null;
        }


        // Get User by Email (normalized: get person, then user)
        public User GetUserByEmail(string email)
        {
            var person = GetPersonByEmail(email);
            if (person == null) return null;
            return GetUserByPersonId(person.PersonId);
        }

        // Get by refresh token (joined with person for email)
        public User GetUserByRefreshToken(string refreshToken)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT U.*, P.Email, P.Name, P.Phone
                FROM Users U
                LEFT JOIN Person P ON U.PersonId = P.PersonId
                WHERE U.RefreshToken = @token AND U.IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@token", refreshToken);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader, joinedPerson: true);
                }
            }
            return null;
        }

        public User GetUserByPasswordResetToken(string token)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT U.*, P.Email, P.Name, P.Phone
                FROM Users U
                LEFT JOIN Person P ON U.PersonId = P.PersonId
                WHERE U.PasswordResetToken = @Token AND U.IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader, joinedPerson: true);
                }
            }
            return null;
        }

        public User GetUserByEmailToken(string token)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT U.*, P.Email, P.Name, P.Phone
                FROM Users U
                LEFT JOIN Person P ON U.PersonId = P.PersonId
                WHERE U.EmailVerificationToken = @Token AND U.IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader, joinedPerson: true);
                }
            }
            return null;
        }

        // Update User (only user fields, not email or person)
        public void UpdateUser(User user)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
        UPDATE Users SET 
            PasswordHash = @PasswordHash,
            Role = @Role,
            RefreshToken = @RefreshToken,
            RefreshTokenExpiry = @RefreshTokenExpiry,
            IsEmailConfirmed = @IsEmailConfirmed,
            EmailVerificationToken = @EmailVerificationToken,
            FailedLoginAttempts = @FailedLoginAttempts,
            LockoutEnd = @LockoutEnd,
            PasswordResetToken = @PasswordResetToken,
            PasswordResetExpiry = @PasswordResetExpiry,
            IsDeleted = @IsDeleted
        WHERE UserId = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RefreshToken", (object)user.RefreshToken ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RefreshTokenExpiry", (object)user.RefreshTokenExpiry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsEmailConfirmed", user.IsEmailConfirmed);
                cmd.Parameters.AddWithValue("@EmailVerificationToken", (object)user.EmailVerificationToken ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FailedLoginAttempts", user.FailedLoginAttempts);
                cmd.Parameters.AddWithValue("@LockoutEnd", (object)user.LockoutEnd ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);
                cmd.Parameters.AddWithValue("@PasswordResetToken", (object)user.PasswordResetToken ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PasswordResetExpiry", (object)user.PasswordResetExpiry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsDeleted", user.IsDeleted);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        // Update Person email/name/phone if you need it!
        public void UpdatePerson(Person person)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                UPDATE Person SET 
                    Name = @Name,
                    Email = @Email,
                    Phone = @Phone
                WHERE PersonId = @PersonId", conn))
            {
                cmd.Parameters.AddWithValue("@Name", person.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", person.Email ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", person.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PersonId", person.PersonId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Set password reset token
        public void SetPasswordResetToken(int userId, string token, DateTime expiry)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"UPDATE Users SET PasswordResetToken=@Token, PasswordResetExpiry=@Expiry WHERE UserId=@UserId", conn))
            {
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@Expiry", expiry);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Optional: Get User by Id (with person join)
        public User GetUserById(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT U.*, P.Email, P.Name, P.Phone
                FROM Users U
                LEFT JOIN Person P ON U.PersonId = P.PersonId
                WHERE U.UserId = @UserId AND U.IsDeleted = 0", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapUser(reader, joinedPerson: true);
                }
            }
            return null;
        }

        // Helper: MapUser
        private User MapUser(IDataReader reader, bool joinedPerson = false)
        {
            return new User
            {
                UserId = (int)reader["UserId"],
                PersonId = (int)reader["PersonId"],
                Username = reader["Username"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                Role = reader["Role"] == DBNull.Value ? null : reader["Role"].ToString(),
                CreatedAt = (DateTime)reader["CreatedAt"],
                RefreshToken = reader["RefreshToken"] == DBNull.Value ? null : reader["RefreshToken"].ToString(),
                RefreshTokenExpiry = reader["RefreshTokenExpiry"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["RefreshTokenExpiry"],
                FailedLoginAttempts = reader["FailedLoginAttempts"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FailedLoginAttempts"]),
                LockoutEnd = reader["LockoutEnd"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["LockoutEnd"],
                IsEmailConfirmed = reader["IsEmailConfirmed"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsEmailConfirmed"]),
                EmailVerificationToken = reader["EmailVerificationToken"] == DBNull.Value ? null : reader["EmailVerificationToken"].ToString(),
                PasswordResetToken = reader["PasswordResetToken"] == DBNull.Value ? null : reader["PasswordResetToken"].ToString(),
                PasswordResetExpiry = reader["PasswordResetExpiry"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["PasswordResetExpiry"],
                IsDeleted = reader["IsDeleted"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsDeleted"]) // new
            };
        }


    }
}
