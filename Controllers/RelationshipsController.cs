using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class RelationshipsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RelationshipsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<MarketplaceRelationshipDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MarketplaceRelationshipDto>>> Get(CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id,
                       relationship_type, verification_status, availability_status, created_at_utc
                FROM marketplace_relationships
                ORDER BY created_at_utc DESC;
                """;

            var relationships = new List<MarketplaceRelationshipDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                relationships.Add(new MarketplaceRelationshipDto(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetString(8),
                    DateTimeOffset.Parse(reader.GetString(9))));
            }

            return relationships;
        }
    }
}
