// Controllers/QuotesController.cs
using Itarix.Api.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;

namespace itarixapi.Controllers
{
    public class CreateQuoteRequest
    {
        [Required] public string Service { get; set; } = "";
        [Required] public string Tier { get; set; } = "";
        [Required] public string Type { get; set; } = "";
        [Range(1, 999)] public int? Pages { get; set; } // Web only
        public List<string> Features { get; set; } = new();
        public List<string>? Platforms { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }
        public bool SaveToAccount { get; set; } = true;
    }

    public class QuoteRecord
    {
        public string QuoteId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string Service { get; set; } = "";
        public string Tier { get; set; } = "";
        public string Type { get; set; } = "";
        public int Pages { get; set; } = 1;
        public List<string> Features { get; set; } = new();
        public List<string> Platforms { get; set; } = new();

        public int EstimatedHours { get; set; }
        public int PriceNumber { get; set; }
        public bool SaveToAccount { get; set; } = true;
        public string? Note { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuotesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _cfg;
        private readonly UserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly PricingService _pricing;

        private string DataFile => Path.Combine(_env.ContentRootPath, "App_Data", "quotes.json");

        public QuotesController(
            IWebHostEnvironment env,
            IConfiguration cfg,
            UserService userService,
            IEmailSender emailSender,
            PricingService pricing)
        {
            _env = env;
            _cfg = cfg;
            _userService = userService;
            _emailSender = emailSender;
            _pricing = pricing;

            var dir = Path.GetDirectoryName(DataFile)!;
            Directory.CreateDirectory(dir);
            if (!System.IO.File.Exists(DataFile)) System.IO.File.WriteAllText(DataFile, "[]");
        }

        private static string NewQuoteId()
        {
            var r = Random.Shared.Next(0, 99999).ToString("D5");
            return $"Q-{DateTime.UtcNow:yyyy}-{r}";
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateQuoteRequest dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("sub")?.Value
                        ?? "";
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var email = User.FindFirstValue(ClaimTypes.Email)
                       ?? User.FindFirst("email")?.Value
                       ?? "";
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Missing user email" });

            if (!PricingService.ValidServices.Contains(dto.Service))
                return BadRequest(new { message = "Invalid service" });
            if (!PricingService.ValidTiers.Contains(dto.Tier))
                return BadRequest(new { message = "Invalid tier" });
            if (string.IsNullOrWhiteSpace(dto.Type))
                return BadRequest(new { message = "Type is required" });

            var pages = dto.Service == "Web Services" ? Math.Max(1, dto.Pages ?? 1) : 1;

            var filtered = _pricing.FilterFeatures(dto.Service, dto.Type, dto.Features);
            var price = _pricing.ComputePrice(dto.Service, dto.Tier, dto.Type, pages, filtered);
            var hours = _pricing.ComputeHours(dto.Service, dto.Tier, pages, filtered);

            var rec = new QuoteRecord
            {
                QuoteId = NewQuoteId(),
                UserId = userId,
                UserEmail = email,
                CreatedAt = DateTime.UtcNow,
                Service = dto.Service,
                Tier = dto.Tier,
                Type = dto.Type,
                Pages = pages,
                Features = filtered,
                Platforms = dto.Platforms ?? new(),
                EstimatedHours = hours,
                PriceNumber = price,
                SaveToAccount = dto.SaveToAccount,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note!.Trim()
            };

            if (rec.SaveToAccount)
            {
                var json = await System.IO.File.ReadAllTextAsync(DataFile);
                var list = JsonSerializer.Deserialize<List<QuoteRecord>>(json) ?? new();
                list.Add(rec);
                await System.IO.File.WriteAllTextAsync(
                    DataFile,
                    JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            await SendEmailAsync(rec);

            Response.Headers["x-quote-id"] = rec.QuoteId;
            return Ok(new { success = true, quoteId = rec.QuoteId, priceNumber = rec.PriceNumber });
        }

        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Mine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirst("sub")?.Value
                        ?? "";
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var json = await System.IO.File.ReadAllTextAsync(DataFile);
            var list = JsonSerializer.Deserialize<List<QuoteRecord>>(json) ?? new();
            var mine = list.Where(x => x.UserId == userId)
                           .OrderByDescending(x => x.CreatedAt);
            return Ok(new { items = mine });
        }

        private async Task SendEmailAsync(QuoteRecord r)
        {
            string requesterName = "Unknown";
            string requesterEmail = r.UserEmail ?? string.Empty;

            if (int.TryParse(r.UserId, out var uid))
            {
                var u = _userService.GetUserById(uid);
                if (u != null)
                {
                    if (!string.IsNullOrWhiteSpace(u.Username)) requesterName = u.Username;
                    var person = _userService.GetPersonById(u.PersonId);
                    if (!string.IsNullOrWhiteSpace(person?.Email)) requesterEmail = person!.Email;
                }
            }

            var features = (r.Features?.Any() == true) ? string.Join(", ", r.Features) : "—";
            var platforms = (r.Platforms?.Any() == true) ? string.Join(", ", r.Platforms!) : null;

            var html = $@"
<div style='font-family:Inter,Segoe UI,Arial,sans-serif;color:#1c2b44'>
  <h2 style='margin:0 0 8px'>Your iTARiX Quote</h2>
  <p style='margin:0 0 12px;color:#52647a'>Reference: <b>{r.QuoteId}</b> · {DateTime.Now}</p>
  <table cellpadding='0' cellspacing='0' style='border-collapse:collapse;max-width:640px'>
    <tr><td style='padding:6px 0'><b>Service</b></td><td>{System.Net.WebUtility.HtmlEncode(r.Service)}</td></tr>
    <tr><td style='padding:6px 0'><b>Tier</b></td><td>{System.Net.WebUtility.HtmlEncode(r.Tier)}</td></tr>
    <tr><td style='padding:6px 0'><b>Type</b></td><td>{System.Net.WebUtility.HtmlEncode(r.Type)}</td></tr>
    {(r.Service == "Web Services" ? $"<tr><td style='padding:6px 0'><b>Pages</b></td><td>{r.Pages}</td></tr>" : "")}
    <tr><td style='padding:6px 0'><b>Features</b></td><td>{System.Net.WebUtility.HtmlEncode(features)}</td></tr>
    {(platforms is null ? "" : $"<tr><td style='padding:6px 0'><b>Platforms</b></td><td>{System.Net.WebUtility.HtmlEncode(platforms)}</td></tr>")}
    <tr><td style='padding:6px 0'><b>Estimated Hours</b></td><td>{r.EstimatedHours}</td></tr>
    <tr><td style='padding:6px 0'><b>Estimate</b></td><td><b>{r.PriceNumber:N0} €</b></td></tr>
  </table>
  {(string.IsNullOrWhiteSpace(r.Note) ? "" :
    $"<p style='margin:12px 0;color:#1c2b44'><b>Note from user:</b> {System.Net.WebUtility.HtmlEncode(r.Note)}</p>")}
  <p style='margin:14px 0 0;color:#52647a'>We’ll contact you shortly to confirm the details.</p>
</div>";

            await _emailSender.SendEmailAsync(r.UserEmail, $"Your iTARiX Quote ({r.QuoteId})", html);

            var teamCopy = _cfg["SMTP:FromEmail"];
            if (!string.IsNullOrWhiteSpace(teamCopy) &&
                !string.Equals(teamCopy, r.UserEmail, StringComparison.OrdinalIgnoreCase))
            {
                await _emailSender.SendEmailAsync(teamCopy!, $"[Copy] iTARiX Quote ({r.QuoteId})", html);
            }
        }
    }
}
