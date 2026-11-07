
using Microsoft.AspNetCore.Mvc;
using OT.Assessment.App.Messaging;
using OT.Assessment.App.Repositories;
using OT.Assessment.Contracts.Message;

namespace OT.Assessment.App.Controllers;

[ApiController]
[Route("api/player")]
public sealed class CasinoWagerController(
    RabbitMqPublisher publisher,
    ICasinoWagerRepository repository,
    ILogger<CasinoWagerController> logger) : ControllerBase
{
    [HttpPost("casinowager")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SubmitCasinoWager(
        [FromBody] CasinoWagerMessage message,
        CancellationToken cancellationToken)
    {
        if (message.WagerId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "WagerId is required."
            });
        }

        try
        {
            await publisher.PublishAsync(message, cancellationToken);

            logger.LogInformation(
                "Casino wager {WagerId} published successfully.",
                message.WagerId);

            return Accepted(new
            {
                Message = "Casino wager accepted for processing.",
                message.WagerId
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish casino wager {WagerId}.",
                message.WagerId);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    Message = "Unable to submit wager for processing."
                });
        }
    }

    [HttpGet("{playerId:guid}/casinowagers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayerCasinoWagers(
        Guid playerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            return BadRequest(new
            {
                Message = "A valid player ID is required."
            });
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                Message = "Page must be greater than zero and pageSize must be between 1 and 100."
            });
        }

        var result = await repository.GetPlayerCasinoWagersAsync(
            playerId,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("topspenders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopSpenders(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 100)
        {
            return BadRequest(new
            {
                Message = "Count must be between 1 and 100."
            });
        }

        var result = await repository.GetTopSpendersAsync(
            count,
            cancellationToken);

        return Ok(result);
    }
}
