using BookSystem.Repoistory;
using BookSystem.Repoistory.Entities;
using BookSystem.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BookSystem.Services
{
    public class BookService : ServiceBase<Book>, IBookService
    {
        private BookContext bookContext;

        public BookService(BookContext bookContext) : base(bookContext)
        {
            this.bookContext = bookContext;
        }
    }
}
