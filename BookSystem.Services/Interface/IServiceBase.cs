using BookSystem.Repoistory.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSystem.Services.Interface
{
    public interface IServiceBase<T> where T : EntityBase
    {
        Task<T> Add(T entity);
        Task Delete(int id);
        Task<T> Update(T entity);
        Task<List<T>> GetAll();
        Task<T> GetById(int id);
    }
}
