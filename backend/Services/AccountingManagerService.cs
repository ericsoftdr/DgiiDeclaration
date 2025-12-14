
using DgiiIntegration.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AccountingManagerApi.Services
{
    public class AccountingManagerService
    {
        private readonly ApplicationDbContext _context;

        public AccountingManagerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AccountingManager>> GetAllAsync()
        {
            return await _context.AccountingManagers.ToListAsync();
        }

        public async Task<AccountingManager?> GetByIdAsync(int id)
        {
            return await _context.AccountingManagers.FindAsync(id);
        }

        public async Task CreateAsync(AccountingManager manager)
        {
            manager.CreatedDate = DateTime.UtcNow; 
            _context.AccountingManagers.Add(manager);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(AccountingManager manager)
        {
            _context.Entry(manager).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var manager = await _context.AccountingManagers.FindAsync(id);
            if (manager != null)
            {
                _context.AccountingManagers.Remove(manager);
                await _context.SaveChangesAsync();
            }
        }
    }
}
