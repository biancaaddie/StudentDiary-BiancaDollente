using Microsoft.EntityFrameworkCore;
using StudentDiary.Infrastructure.Data;
using StudentDiary.Infrastructure.Models;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace StudentDiary.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly StudentDiaryContext _context;

        public AuthService(StudentDiaryContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == registerDto.Username);
                if (existingUser != null)
                {
                    return (false, "Username already exists.");
                }

                // Check if email already exists
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
                if (existingEmail != null)
                {
                    return (false, "Email already registered.");
                }

                // Create new user
                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = HashPassword(registerDto.Password),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    DateCreated = DateTime.UtcNow,
                    FailedLoginAttempts = 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, "Registration successful.");
            }
            catch (Exception ex)
            {
                return (false, $"Registration failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message, UserProfileDto User)> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null)
                {
                    return (false, "Invalid username or password.", null);
                }

                // Check if account is locked
                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                {
                    var lockoutMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                    return (false, $"Account is locked. Try again in {lockoutMinutes} minutes.", null);
                }

                // Verify password
                if (!VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    // Increment failed login attempts
                    user.FailedLoginAttempts++;
                    
                    // Lock account after 3 failed attempts
                    if (user.FailedLoginAttempts >= 3)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15); // 15 minute lockout
                        await _context.SaveChangesAsync();
                        return (false, "Account locked due to multiple failed login attempts. Try again in 15 minutes.", null);
                    }

                    await _context.SaveChangesAsync();
                    return (false, $"Invalid username or password. {3 - user.FailedLoginAttempts} attempts remaining.", null);
                }

                // Reset failed login attempts and update last login
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var userProfile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProfilePicturePath = user.ProfilePicturePath,
                    DateCreated = user.DateCreated
                };

                return (true, "Login successful.", userProfile);
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);

                if (user == null)
                {
                    // Don't reveal if email exists for security reasons
                    return (true, "If the email exists, a password reset link has been sent.");
                }

                // Generate reset token
                user.PasswordResetToken = GenerateResetToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

                await _context.SaveChangesAsync();

                // In a real application, you would send an email here
                // For this demo, we'll just return success
                return (true, "If the email exists, a password reset link has been sent.");
            }
            catch (Exception ex)
            {
                return (false, $"Password reset failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == resetPasswordDto.Token &&
                                             u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return (false, "Invalid or expired reset token.");
                }

                // Update password and clear reset token
                user.PasswordHash = HashPassword(resetPasswordDto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.FailedLoginAttempts = 0; // Reset failed attempts
                user.LockoutEnd = null; // Remove any lockout

                await _context.SaveChangesAsync();

                return (true, "Password reset successful.");
            }
            catch (Exception ex)
            {
                return (false, $"Password reset failed: {ex.Message}");
            }
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return null;

                return new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProfilePicturePath = user.ProfilePicturePath,
                    DateCreated = user.DateCreated
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId, UpdateProfileDto updateProfileDto)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                // Check if email is already taken by another user
                if (!string.IsNullOrEmpty(updateProfileDto.Email) && updateProfileDto.Email != user.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == updateProfileDto.Email && u.Id != userId);
                    if (emailExists)
                    {
                        return (false, "Email is already taken.");
                    }
                    user.Email = updateProfileDto.Email;
                }

                // Update profile information
                if (!string.IsNullOrEmpty(updateProfileDto.FirstName))
                    user.FirstName = updateProfileDto.FirstName;
                
                if (!string.IsNullOrEmpty(updateProfileDto.LastName))
                    user.LastName = updateProfileDto.LastName;

                await _context.SaveChangesAsync();
                return (true, "Profile updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Profile update failed: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateProfilePictureAsync(int userId, string imagePath)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return (false, "User not found.");
                }

                user.ProfilePicturePath = imagePath;
                await _context.SaveChangesAsync();

                return (true, "Profile picture updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Profile picture update failed: {ex.Message}");
            }
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "StudentDiarySalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        private string GenerateResetToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
