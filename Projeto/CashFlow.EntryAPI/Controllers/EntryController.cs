using CashFlow.Application.UseCases.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.EntryAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EntryController : ControllerBase
{
    private readonly CreateEntryCommandHandler _handler;
    private readonly ILogger<EntryController> _logger;

    public EntryController(CreateEntryCommandHandler handler, ILogger<EntryController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateEntryCommand command)
    {
        try
        {
            // Validações básicas de entrada
            if (command == null || string.IsNullOrWhiteSpace(command.Description) || command.Amount <= 0)
            {
                return BadRequest(new { Error = "Dados de entrada inválidos. Verifique a descrição e o valor." });
            }

            _logger.LogInformation("Recebido novo pedido de lançamento financeiro.");

            // Orquestra a regra de negócio (Grava no SQL e publica no Kafka)
            var entryId = await _handler.Handle(command);

            _logger.LogInformation($"Lançamento {entryId} processado com sucesso.");

            // Retorna HTTP 201 Created com o ID gerado
            return Created(string.Empty, new { Id = entryId, Message = "Lançamento registrado com sucesso." });
        }
        catch (ArgumentException ex)
        {
            // Erros de domínio (ex: descrição vazia na entidade)
            _logger.LogWarning($"Erro de validação: {ex.Message}");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro interno crítico: {ex.Message}");

            return StatusCode(500, new { Error = $"ERRO REAL: {ex.Message} | DETALHE: {ex.InnerException?.Message}" });
        }
    }
}