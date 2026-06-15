using DJBrate.Domain.Interfaces;
using DJBrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DJBrate.Infrastructure.Repositories;

// Implementira osnovne CRUD operacije za sve EF Core entitete.
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // Dohvaca jedan zapis prema primarnom ID-u.
    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    // Dohvaca sve zapise odredenog tipa.
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) //preko AppDbContext sprema promjene u bazu. 
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity) //preko AppDbContext sprema promjene u bazu. 
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity) //preko AppDbContext sprema promjene u bazu.
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
