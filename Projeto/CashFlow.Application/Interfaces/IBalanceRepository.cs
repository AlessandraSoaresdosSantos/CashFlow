using System.Threading.Tasks;

namespace CashFlow.Application.Interfaces;

public interface IBalanceRepository
{
    Task<double> GetBalanceAsync(string date);
}