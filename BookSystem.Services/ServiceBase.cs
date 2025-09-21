using BookSystem.Repoistory;
using BookSystem.Repoistory.Entities;
using BookSystem.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSystem.Services
{
    public class ServiceBase<T> : IServiceBase<T> where T : EntityBase
    {
        private readonly BookContext _context;
        public DbSet<T> entity => _context.Set<T>();
        public ServiceBase(BookContext appContext)
        {
            this._context = appContext;
        }

        public async Task<T> Add(T entity)
        {
            this.entity.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task Delete(int id)
        {
            var enetity = await this.GetById(id);
            this.entity.Remove(enetity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<T>> GetAll()
        {
            return await this.entity.ToListAsync();
        }

        public async Task<T> GetById(int id)
        {
            return await _context.FindAsync<T>(id);
        }

        public async Task<T> Update(T entity)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();

            return entity;
        }
    }
}
