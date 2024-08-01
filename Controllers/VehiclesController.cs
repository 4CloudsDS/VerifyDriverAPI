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
    public class VehiclesController: ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiclesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            Console.WriteLine("GET: api/Vehicles called");
            //return await _context.Vehicles.ToListAsync();
            return await _context.Vehicles
                .Include(v => v.Platform)
                .Include(v => v.Partner)
                .ToListAsync();
        }

        // GET: api/Vehicles/*
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Platform)
                .Include(v => v.Partner)
                .FirstOrDefaultAsync(v => v.vID == id);

            if (vehicle == null)
            {
                return NotFound();
            }
            
            return vehicle;
        }

        // POST :api/Vehicles
        [HttpPost]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicle), new {id=vehicle.vID}, vehicle);
        }

        //PUT: api/Users/*
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.vID)
            {
                return BadRequest();
            }

           _context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException)
            {
                if(!VehicleExists(id))
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

        // DELETE: api/Vehicles/*
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle= await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return  NoContent();
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(v => v.vID == id);
        }
    }

}
