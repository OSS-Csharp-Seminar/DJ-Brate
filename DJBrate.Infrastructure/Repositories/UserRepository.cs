using DJBrate.Domain.Entities;
using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    // Dohvaca korisnika prema email adresi.
    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetBySpotifyIdAsync(string spotifyId) //pretrazuje user tablicu te vraca korisnika koji ima taj SpotifyId, ili null ako ne postoji. (koristi se u UserService i Program.cs)
        => await _dbSet.FirstOrDefaultAsync(u => u.SpotifyId == spotifyId);
}
