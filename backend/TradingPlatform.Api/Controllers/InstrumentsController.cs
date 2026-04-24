    namespace TradingPlatform.Api.Controllers;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using TradingPlatform.Core.Dtos;
    using TradingPlatform.Core.Interfaces;

    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public sealed class InstrumentsController : ControllerBase
    {
        private readonly IInstrumentService _instrumentService;

        public InstrumentsController(IInstrumentService instrumentService)
        {
            _instrumentService = instrumentService;
        }

        /// <summary>
        /// Get all instruments (including blocked ones)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAll(CancellationToken cancellationToken)
        {
            var instruments = await _instrumentService.GetAllAsync(
                page: 1,
                pageSize: 50,
                cancellationToken: cancellationToken);

            return Ok(instruments);
        }

        /// <summary>
        /// Get only active and not blocked instruments (PUBLIC)
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAllActive(CancellationToken cancellationToken)
        {
            var instruments = await _instrumentService.GetAllActiveAsync(cancellationToken);
            return Ok(instruments);
        }

        /// <summary>
        /// Get instrument by ID (PUBLIC)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<InstrumentDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var instrument = await _instrumentService.GetByIdAsync(id, cancellationToken);
            return Ok(instrument);
        }

        /// <summary>
        /// Get instrument by symbol (PUBLIC)
        /// </summary>
        [HttpGet("symbol/{symbol}")]
        [AllowAnonymous]
        public async Task<ActionResult<InstrumentDto>> GetBySymbol(string symbol, CancellationToken cancellationToken)
        {
            var instrument = await _instrumentService.GetBySymbolAsync(symbol, cancellationToken);
            return Ok(instrument);
        }

        /// <summary>
        /// Create new instrument (ADMIN only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InstrumentDto>> Create(
            [FromBody] CreateInstrumentRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            var instrument = await _instrumentService.RequestCreateAsync(request, adminId, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = instrument.Id }, instrument);
        }

        /// <summary>
        /// Request update (ADMIN only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<InstrumentDto>> RequestUpdate(
            Guid id,
            [FromBody] UpdateInstrumentRequest request,
            CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            var result = await _instrumentService.RequestUpdateAsync(id, request, adminId, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Request block (ADMIN only)
        /// </summary>
        [HttpPatch("{id}/block")]
        public async Task<ActionResult<InstrumentDto>> RequestBlock(
            Guid id,
            [FromBody] AdminRequestReasonRequest reason,
            CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            var result = await _instrumentService.RequestBlockAsync(id, reason.Reason, adminId, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Request unblock (ADMIN only)
        /// </summary>
        [HttpPatch("{id}/unblock")]
        public async Task<ActionResult<InstrumentDto>> RequestUnblock(
            Guid id,
            [FromBody] AdminRequestReasonRequest reason,
            CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            var result = await _instrumentService.RequestUnblockAsync(id, reason.Reason, adminId, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Request delete (ADMIN only)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RequestDelete(Guid id, CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            await _instrumentService.RequestDeleteAsync(id, adminId, cancellationToken);

            return Accepted();
        }

        /// <summary>
        /// Request approval (ADMIN only)
        /// </summary>
        [HttpPost("{id}/request-approval")]
        public async Task<ActionResult<InstrumentDto>> RequestApproval(Guid id, CancellationToken cancellationToken)
        {
            var adminId = GetAdminId();
            var instrument = await _instrumentService.RequestApprovalAsync(id, adminId, cancellationToken);

            return Ok(instrument);
        }

        // ===== helper =====

        private Guid GetAdminId()
        {
            var id = User.FindFirst("sub")?.Value
                    ?? throw new InvalidOperationException("User ID not found in token");

            return Guid.Parse(id);
        }
    }