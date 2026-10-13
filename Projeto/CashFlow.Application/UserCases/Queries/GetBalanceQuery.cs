using System.Threading.Tasks;
using CashFlow.Application.Interfaces;

namespace CashFlow.Application.UseCases.Queries;

// O Input da consulta (Apenas a data)
public record GetBalanceQuery(string Date);

// O orquestrador da consulta
public class GetBalanceQueryHandler
{
    private readonly IBalanceRepository _repository;

    public GetBalanceQueryHandler(IBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<double> Handle(GetBalanceQuery query)
    {
        // Aqui poderíamos ter lógicas extra, mas no caso do saldo, 
        // basta delegar a busca rápida para o repositório.
        return await _repository.GetBalanceAsync(query.Date);
    }
}