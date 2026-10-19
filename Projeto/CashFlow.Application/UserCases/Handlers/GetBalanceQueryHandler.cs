using System.Threading.Tasks;
using CashFlow.Application.Interfaces;
using CashFlow.Application.UseCases.Queries;

namespace CashFlow.Application.UseCases.Handlers;

public class GetBalanceQueryHandler : IGetBalanceQueryHandler
{
    private readonly IBalanceRepository _balanceRepository;

    public GetBalanceQueryHandler(IBalanceRepository balanceRepository)
    {
        _balanceRepository = balanceRepository;
    }

    public async Task<decimal> Handle(GetBalanceQuery query)
    {
        return await _balanceRepository.GetBalanceAsync(query.Date);
    }
}