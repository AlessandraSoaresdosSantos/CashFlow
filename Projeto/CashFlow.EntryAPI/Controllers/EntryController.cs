using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CashFlow.Application.UseCases.Commands;
using CashFlow.Application.Interfaces;

namespace CashFlow.EntryAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntryController : ControllerBase
{
    private readonly ICreateEntryCommandHandler _commandHandler;

    public EntryController(ICreateEntryCommandHandler commandHandler)
    {
        _commandHandler = commandHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEntryCommand command)
    {
        if (command == null)
        {
            return BadRequest("O payload do lançamento não pode ser nulo.");
        }

        try
        {
           var entryId = await _commandHandler.Handle(command);

           return StatusCode(201, new { Id = entryId, Message = "Lançamento recebido com sucesso!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Erro interno ao processar o lançamento financeiro.", Detalhe = ex.Message });
        }
    }
}