using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Services;
using ToDoApi.Data;
using ToDoApi.Models;

namespace ToDoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToDosController : ControllerBase
    {
        private readonly ToDoContext _context;
        private readonly RabbitMqServiceProducer _rabbitMqService;
        public ToDosController(ToDoContext context, RabbitMqServiceProducer rabbitMqService)
        {
            _context = context;
            _rabbitMqService = rabbitMqService;
        }

        // GET: api/ToDos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToDo>>> GetToDos()
        {
            return await _context.ToDos.ToListAsync();
        }

        // GET: api/ToDos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ToDo>> GetToDo(int id)
        {
            var toDo = await _context.ToDos.FindAsync(id);

            if (toDo == null)
            {
                return NotFound();
            }

            return toDo;
        }

        // POST: api/ToDos
        [HttpPost]
        public async Task<ActionResult<ToDo>> PostToDo(ToDo toDo)
        {
            toDo.CreatedOn = ConvertToUtc(toDo.CreatedOn);
            toDo.DueDate = ConvertToUtc(toDo.DueDate);

            _context.ToDos.Add(toDo);
            await _context.SaveChangesAsync();

            var message = $"New ToDo created: {toDo.Title}";
            _rabbitMqService.SendMessage(toDo);
           
            return CreatedAtAction("GetToDo", new { id = toDo.Id }, toDo);
        }

        private DateTime ConvertToUtc(DateTime dateTime)
        {
            // Convert Local or Unspecified DateTime to UTC
            if (dateTime.Kind == DateTimeKind.Local || dateTime.Kind == DateTimeKind.Unspecified)
            {
                return dateTime.ToUniversalTime();
            }
            return dateTime; // Already UTC
        }

        // PUT: api/ToDos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutToDo(int id, ToDo toDo)
        {
            if (id != toDo.Id)
            {
                return BadRequest();
            }

            _context.Entry(toDo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ToDoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ToDos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteToDo(int id)
        {
            var toDo = await _context.ToDos.FindAsync(id);
            if (toDo == null)
            {
                return NotFound();
            }

            _context.ToDos.Remove(toDo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ToDoExists(int id)
        {
            return _context.ToDos.Any(e => e.Id == id);
        }
    }
}