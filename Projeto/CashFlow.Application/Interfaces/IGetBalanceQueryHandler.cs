using System.Threading.Tasks;
using CashFlow.Application.UseCases.Queries;  

namespace CashFlow.Application.Interfaces;

public interface IGetBalanceQueryHandler
{
    Task<decimal> Handle(GetBalanceQuery query);
}