using System.Threading.Tasks;

namespace CashFlow.Application.Interfaces;

public interface IBalanceRepository
{
    Task<decimal> GetBalanceAsync(string parameter);
}