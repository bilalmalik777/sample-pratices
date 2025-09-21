using BookSystem.Repoistory;
using BookSystem.Repoistory.Entities;
using BookSystem.Services.Interface;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BookSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookService bookService;

        public BooksController(IBookService bookService)
        {
            this.bookService = bookService;
        }

        // GET: api/<BooksController>
        [HttpGet]
        public async Task<IEnumerable<Book>> Get()
        {
            return await this.bookService.GetAll();
        }

        // GET api/<BooksController>/5
        [HttpGet("{id}")]
        public async Task<Book> Get(int id)
        {
            return await this.bookService.GetById(id);
        }

        // POST api/<BooksController>
        [HttpPost]
        public async Task<Book> Post([FromBody]Book entity)
        {
            return await this.bookService.Add(entity);
        }

        // PUT api/<BooksController>/5
        [HttpPut()]
        public async Task<Book> Put(int id, [FromBody] Book entity)
        {
            return await this.bookService.Update(entity);
        }

        // DELETE api/<BooksController>/5
        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {
            await this.bookService.Delete(id);
        }
    }
}
