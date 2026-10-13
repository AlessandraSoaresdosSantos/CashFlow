using System.Threading.Tasks;
using CashFlow.Application.UseCases.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.ConsolidationAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BalanceController : ControllerBase
{
    private readonly GetBalanceQueryHandler _handler;

    // Recebe o Orquestrador da Query por injeção
    public BalanceController(GetBalanceQueryHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> Get(string date)
    {
        // Prepara a Query
        var query = new GetBalanceQuery(date);

        // Executa a busca no Redis
        var balance = await _handler.Handle(query);

        // Devolve o saldo em formato JSON (HTTP 200 OK)
        return Ok(new
        {
            Date = date,
            Balance = balance
        });
    }
}