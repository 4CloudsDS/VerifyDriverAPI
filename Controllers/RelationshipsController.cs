using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class RelationshipsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;

        public RelationshipsController(AppDbContext context, ICurrentUserContext currentUser)
        {
            _context = context;
            _currentUser = currentUser;
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

        [HttpGet("mine")]
        [ProducesResponseType(typeof(IReadOnlyList<MarketplaceRelationshipDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<MarketplaceRelationshipDto>>> GetMine(CancellationToken cancellationToken)
        {
            var relationships = (await GetRelationshipsAsync(
                "WHERE driver_user_id = @userId OR owner_user_id = @userId",
                command =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@userId";
                    parameter.Value = _currentUser.UserId;
                    command.Parameters.Add(parameter);
                },
                cancellationToken)).ToList();

            return relationships;
        }

        [HttpPatch("{relationshipId}")]
        [ProducesResponseType(typeof(MarketplaceRelationshipDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MarketplaceRelationshipDto>> Patch(
            string relationshipId,
            RelationshipUpdateRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.VerificationStatus)
                || string.IsNullOrWhiteSpace(request.AvailabilityStatus))
            {
                return Problem(title: "Invalid relationship update.", detail: "VerificationStatus and AvailabilityStatus are required.", statusCode: StatusCodes.Status400BadRequest);
            }

            var relationship = (await GetRelationshipsAsync(
                "WHERE relationship_id = @relationshipId",
                command =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@relationshipId";
                    parameter.Value = relationshipId;
                    command.Parameters.Add(parameter);
                },
                cancellationToken)).FirstOrDefault();

            if (relationship is null)
            {
                return Problem(title: "Relationship not found.", detail: $"No relationship exists for id {relationshipId}.", statusCode: StatusCodes.Status404NotFound);
            }

            if (relationship.DriverUserId != _currentUser.UserId && relationship.OwnerUserId != _currentUser.UserId)
            {
                return Problem(title: "Relationship update not authorized.", detail: "Current user must be a relationship participant.", statusCode: StatusCodes.Status403Forbidden);
            }

            if (IsActive(request.AvailabilityStatus)
                && relationship.OwnerUserId.HasValue
                && await DriverHasAnotherActiveOwnerRelationshipAsync(relationship.DriverUserId, relationship.RelationshipId, cancellationToken))
            {
                return Problem(title: "Relationship rule failed.", detail: "A driver can have only one active owner relationship.", statusCode: StatusCodes.Status400BadRequest);
            }

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE marketplace_relationships
                SET verification_status = {request.VerificationStatus.Trim()},
                    availability_status = {request.AvailabilityStatus.Trim()}
                WHERE relationship_id = {relationshipId};
                """,
                cancellationToken);

            var updated = (await GetRelationshipsAsync(
                "WHERE relationship_id = @relationshipId",
                command =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@relationshipId";
                    parameter.Value = relationshipId;
                    command.Parameters.Add(parameter);
                },
                cancellationToken)).First();

            return updated;
        }

        [HttpDelete("{relationshipId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string relationshipId, CancellationToken cancellationToken)
        {
            var relationship = (await GetRelationshipsAsync(
                "WHERE relationship_id = @relationshipId",
                command =>
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@relationshipId";
                    parameter.Value = relationshipId;
                    command.Parameters.Add(parameter);
                },
                cancellationToken)).FirstOrDefault();

            if (relationship is null)
            {
                return Problem(title: "Relationship not found.", detail: $"No relationship exists for id {relationshipId}.", statusCode: StatusCodes.Status404NotFound);
            }

            if (relationship.DriverUserId != _currentUser.UserId && relationship.OwnerUserId != _currentUser.UserId)
            {
                return Problem(title: "Relationship delete not authorized.", detail: "Current user must be a relationship participant.", statusCode: StatusCodes.Status403Forbidden);
            }

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM marketplace_relationships WHERE relationship_id = {relationshipId};",
                cancellationToken);

            return NoContent();
        }

        private async Task<IReadOnlyList<MarketplaceRelationshipDto>> GetRelationshipsAsync(
            string whereClause,
            Action<System.Data.Common.DbCommand>? configure,
            CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                $"""
                SELECT relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id,
                       relationship_type, verification_status, availability_status, created_at_utc
                FROM marketplace_relationships
                {whereClause}
                ORDER BY created_at_utc DESC;
                """;
            configure?.Invoke(command);

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

        private async Task<bool> DriverHasAnotherActiveOwnerRelationshipAsync(
            int driverUserId,
            string relationshipId,
            CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(1)
                FROM marketplace_relationships
                WHERE driver_user_id = @driverUserId
                  AND relationship_id <> @relationshipId
                  AND owner_user_id IS NOT NULL
                  AND availability_status = 'Active';
                """;
            var driverParameter = command.CreateParameter();
            driverParameter.ParameterName = "@driverUserId";
            driverParameter.Value = driverUserId;
            command.Parameters.Add(driverParameter);
            var relationshipParameter = command.CreateParameter();
            relationshipParameter.ParameterName = "@relationshipId";
            relationshipParameter.Value = relationshipId;
            command.Parameters.Add(relationshipParameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && Convert.ToInt32(result) > 0;
        }

        private static bool IsActive(string availabilityStatus)
        {
            return availabilityStatus.Equals("Active", StringComparison.OrdinalIgnoreCase);
        }
    }
}
