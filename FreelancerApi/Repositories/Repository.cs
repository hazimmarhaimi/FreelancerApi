using FreelancerAPI.Data;
using FreelancerAPI.Dtos;
using FreelancerAPI.Entities;
using FreelancerAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace FreelancerAPI.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    //public async Task<T?> GetByIdAsync(int id)
    //{
    //    var data = await

    //    return await _dbSet.FindAsync(id);
    //}

    public async Task<T?> GetByIdAsync(int id)
    {
        if (typeof(T) == typeof(Freelancer))
        {
            var data = await _context.Freelancers
                .Include(f => f.Skillsets)
                .Include(f => f.Hobbies)
                .FirstOrDefaultAsync(f => f.Id == id);
            return data as T;
        }
        return await _dbSet.FindAsync(id) as T;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}