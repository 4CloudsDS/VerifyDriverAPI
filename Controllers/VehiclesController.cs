using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Models;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController: ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserWorkspaceService _workspace;

        public VehiclesController(AppDbContext context, ICurrentUserWorkspaceService workspace)
        {
            _context = context;
            _workspace = workspace;
        }

        [HttpGet("mine")]
        [ProducesResponseType(typeof(IReadOnlyList<VehicleWorkspaceDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<VehicleWorkspaceDto>>> GetMine(CancellationToken cancellationToken)
        {
            return Ok(await _workspace.GetVehiclesAsync(cancellationToken));
        }

        [HttpPost("mine")]
        [ProducesResponseType(typeof(VehicleWorkspaceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<VehicleWorkspaceDto>> AddMine(
            UpsertVehicleRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var vehicle = await _workspace.AddVehicleAsync(request, cancellationToken);
                return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.VehicleId }, vehicle);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid vehicle update.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPut("mine/{id:int}")]
        [ProducesResponseType(typeof(VehicleWorkspaceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VehicleWorkspaceDto>> UpdateMine(
            int id,
            UpsertVehicleRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var vehicle = await _workspace.UpdateVehicleAsync(id, request, cancellationToken);
                return vehicle is null
                    ? Problem(title: "Vehicle not found.", detail: $"No vehicle exists for id {id}.", statusCode: StatusCodes.Status404NotFound)
                    : vehicle;
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid vehicle update.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(title: "Vehicle update not authorized.", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
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
