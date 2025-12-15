using Google.Api.Ads.AdWords.v201809;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Application.DTOs.Band;
using Podium.Application.Interfaces;
using Podium.Application.Services;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BandController : ControllerBase
    {
        private readonly IBandService _bandService;

        public BandController(IBandService bandService)
        {
            _bandService = bandService;
        }
        /// <summary>
        /// Get a list of all active bands. Accessible by Students and public (if allowed).
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Or [Authorize] depending on your requirements
        public async Task<ActionResult<IEnumerable<BandSummaryDto>>> GetAllBands([FromQuery] BandFilterDto filter)
        {
            var result = await _bandService.GetActiveBandsAsync(filter);
            return HandleResult(result);
        }

        /// <summary>
        /// Get details for a specific band.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<BandDetailDto>> GetBand(int id)
        {
            var result = await _bandService.GetBandDetailsAsync(id);
            return HandleResult(result);
        }

        private ActionResult HandleResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Data == null ? NoContent() : Ok(result.Data);
            }

            return result.ResultType switch
            {
                ServiceResultType.NotFound => NotFound(result.ErrorMessage),
                ServiceResultType.Forbidden => Forbid(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };
        }
    }
}
