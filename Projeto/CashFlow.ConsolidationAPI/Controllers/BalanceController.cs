using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Threading.Tasks;

namespace CashFlow.ConsolidationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BalanceController : ControllerBase
{
    private readonly IGetBalanceQueryHandler _handler;

    // Recebe o Orquestrador da Query por injeção
    public BalanceController(IGetBalanceQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> Get(string date)
    {
        // Validação estrita do formato da data (yyyy-MM-dd)
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return BadRequest(new { Error = "Formato de data inválido. Utilize o formato yyyy-MM-dd." });
        }

        
        var query = new GetBalanceQuery(date);

        var balance = await _handler.Handle(query);

        return Ok(new
        {
            Date = date,
            Balance = balance
        });
    }
}