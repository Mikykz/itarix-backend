using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Itarix.Api.Business;
using Itarix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ITConsultationController : ControllerBase
{
    private readonly ITConsultationService _service;

    public ITConsultationController(ITConsultationService service)
    {
        _service = service;
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var idClaim = User.Claims.FirstOrDefault(c =>
            c.Type == "userId" ||
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type == "sub");

        return idClaim != null && int.TryParse(idClaim.Value, out userId);
    }

    // --------- CREATE (user) ---------
    [Authorize]
    [HttpPost]
    public IActionResult AddConsultation([FromBody] ConsultationDto dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "UserId claim is missing or invalid." });

        var errors = new List<string>();
        if (dto == null) errors.Add("Consultation data is required.");
        if (dto != null && dto.ServiceTypeId <= 0) errors.Add("Valid ServiceTypeId is required and must be > 0.");
        if (dto != null && (dto.Answers == null || dto.Answers.Count == 0)) errors.Add("At least one answer is required.");

        if (errors.Any())
        {
            return BadRequest(new
            {
                message = "Validation failed.",
                errors,
                payload = dto // remove in production if you don't want to echo payloads
            });
        }

        var consultationId = _service.AddConsultationWithAnswers(dto, userId);

        return CreatedAtAction(nameof(GetConsultationById),
            new { id = consultationId },
            new { consultationId });
    }

    // --------- LIST (user) ---------
    // Returns items + total for easy pagination on the client.
    [Authorize]
    [HttpGet("mine")]
    public IActionResult GetMyConsultations(
        [FromQuery] int limit = 25,
        [FromQuery] int offset = 0,
        [FromQuery] string status = null,
        [FromQuery] int? serviceTypeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "UserId claim is missing or invalid." });

        if (limit <= 0) limit = 25;
        if (limit > 200) limit = 200;
        if (offset < 0) offset = 0;

        var items = _service.GetConsultationsByUserPaged(userId, limit, offset, status, serviceTypeId, from, to);
        var total = _service.GetConsultationsByUserCount(userId, status, serviceTypeId, from, to);

        return Ok(new { items, total, limit, offset });
    }

    // Optional: count-only (if you want a lighter request)
    [Authorize]
    [HttpGet("mine/count")]
    public IActionResult GetMyConsultationsCount(
        [FromQuery] string status = null,
        [FromQuery] int? serviceTypeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "UserId claim is missing or invalid." });

        var total = _service.GetConsultationsByUserCount(userId, status, serviceTypeId, from, to);
        return Ok(new { total });
    }

    // --------- DETAIL (user) ---------
    [Authorize]
    [HttpGet("mine/{id:int}")]
    public IActionResult GetMyConsultationById([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "UserId claim is missing or invalid." });

        var consultation = _service.GetConsultationDetailsByIdForUser(id, userId);
        if (consultation == null) return NotFound();
        return Ok(consultation);
    }

    // --------- ADMIN ---------
    [Authorize(Roles = "admin")]
    [HttpGet("user/{userId:int}")]
    public IActionResult GetConsultationsByUser([FromRoute] int userId)
    {
        var consultations = _service.GetConsultationsByUser(userId);
        return Ok(consultations);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("{id:int}")]
    public IActionResult GetConsultationById([FromRoute] int id)
    {
        var consultation = _service.GetConsultationDetailsById(id);
        if (consultation == null) return NotFound();
        return Ok(consultation);
    }
}
