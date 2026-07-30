using CinemaSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CinemaSystem.DAL.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
        Task SaveChangesAsync();

    }
}
