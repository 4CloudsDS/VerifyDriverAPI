using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Models;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserTypesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserTypesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/UserTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserType>>> GetUserTypes()
        {
            Console.WriteLine("GET: api/UserTypes called");
            return await _context.UserTypes.ToListAsync();

        }

        // GET: api/UserTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserType>> GetUserType(int id)
        {
            var usertype = await _context.UserTypes.FindAsync(id);

            if (usertype == null)
            {
                return NotFound();
            }

            return usertype;
        }

        // POST: api/UserTypes
        [HttpPost]
        public async Task<ActionResult<UserType>> PostUserType(UserType usertype)
        {
            _context.UserTypes.Add(usertype);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserType), new { id = usertype.U_T_ID }, usertype);
        }

        // PUT: api/UserTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserType(int id, UserType usertype)
        {
            if (id != usertype.U_T_ID)
            {
                return BadRequest();
            }

            _context.Entry(usertype).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserTypeExists(id))
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

        // DELETE: api/UserTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserType(int id)
        {
            var usertype = await _context.UserTypes.FindAsync(id);
            if (usertype == null)
            {
                return NotFound();
            }

            _context.UserTypes.Remove(usertype);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserTypeExists(int id)
        {
            return _context.UserTypes.Any(e => e.U_T_ID == id);
        }
    }
}
