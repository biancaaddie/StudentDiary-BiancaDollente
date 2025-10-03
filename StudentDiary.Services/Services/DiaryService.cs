using Microsoft.EntityFrameworkCore;
using StudentDiary.Infrastructure.Data;
using StudentDiary.Infrastructure.Models;
using StudentDiary.Services.DTOs;
using StudentDiary.Services.Interfaces;

namespace StudentDiary.Services.Services
{
    public class DiaryService : IDiaryService
    {
        private readonly StudentDiaryContext _context;

        public DiaryService(StudentDiaryContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DiaryEntryDto>> GetUserEntriesAsync(int userId)
        {
            try
            {
                var entries = await _context.DiaryEntries
                    .Where(e => e.UserId == userId)
                    .OrderByDescending(e => e.CreatedDate)
                    .Select(e => new DiaryEntryDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Content = e.Content,
                        CreatedDate = e.CreatedDate,
                        LastModifiedDate = e.LastModifiedDate,
                        UserId = e.UserId
                    })
                    .ToListAsync();

                return entries;
            }
            catch (Exception)
            {
                return new List<DiaryEntryDto>();
            }
        }

        public async Task<DiaryEntryDto> GetEntryByIdAsync(int entryId, int userId)
        {
            try
            {
                var entry = await _context.DiaryEntries
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

                if (entry == null)
                    return null;

                return new DiaryEntryDto
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Content = entry.Content,
                    CreatedDate = entry.CreatedDate,
                    LastModifiedDate = entry.LastModifiedDate,
                    UserId = entry.UserId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message, DiaryEntryDto Entry)> CreateEntryAsync(int userId, CreateDiaryEntryDto createDto)
        {
            try
            {
                // Validate that the user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return (false, "User not found.", null);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(createDto.Title))
                {
                    return (false, "Title is required.", null);
                }

                if (string.IsNullOrWhiteSpace(createDto.Content))
                {
                    return (false, "Content is required.", null);
                }

                var entry = new DiaryEntry
                {
                    Title = createDto.Title.Trim(),
                    Content = createDto.Content.Trim(),
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                _context.DiaryEntries.Add(entry);
                await _context.SaveChangesAsync();

                var entryDto = new DiaryEntryDto
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Content = entry.Content,
                    CreatedDate = entry.CreatedDate,
                    LastModifiedDate = entry.LastModifiedDate,
                    UserId = entry.UserId
                };

                return (true, "Diary entry created successfully.", entryDto);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to create diary entry: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, DiaryEntryDto Entry)> UpdateEntryAsync(int userId, UpdateDiaryEntryDto updateDto)
        {
            try
            {
                var entry = await _context.DiaryEntries
                    .FirstOrDefaultAsync(e => e.Id == updateDto.Id && e.UserId == userId);

                if (entry == null)
                {
                    return (false, "Diary entry not found or you don't have permission to edit it.", null);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(updateDto.Title))
                {
                    return (false, "Title is required.", null);
                }

                if (string.IsNullOrWhiteSpace(updateDto.Content))
                {
                    return (false, "Content is required.", null);
                }

                // Update the entry
                entry.Title = updateDto.Title.Trim();
                entry.Content = updateDto.Content.Trim();
                entry.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var entryDto = new DiaryEntryDto
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Content = entry.Content,
                    CreatedDate = entry.CreatedDate,
                    LastModifiedDate = entry.LastModifiedDate,
                    UserId = entry.UserId
                };

                return (true, "Diary entry updated successfully.", entryDto);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to update diary entry: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteEntryAsync(int entryId, int userId)
        {
            try
            {
                var entry = await _context.DiaryEntries
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

                if (entry == null)
                {
                    return (false, "Diary entry not found or you don't have permission to delete it.");
                }

                _context.DiaryEntries.Remove(entry);
                await _context.SaveChangesAsync();

                return (true, "Diary entry deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to delete diary entry: {ex.Message}");
            }
        }
    }
}
