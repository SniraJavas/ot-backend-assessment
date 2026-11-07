
using Microsoft.AspNetCore.Mvc;
using OT.Assessment.App.Messaging;
using OT.Assessment.Contracts.Message;

namespace OT.Assessment.App.Controllers;

[ApiController]
[Route("api/player")]
public sealed class CasinoWagerController(
    RabbitMqPublisher publisher,
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
        if (string.IsNullOrWhiteSpace(message.WagerId))
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
}
