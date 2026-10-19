using System.Threading.Tasks;
using CashFlow.Application.UseCases.Queries;  

namespace CashFlow.Application.Interfaces;

public interface IGetBalanceQueryHandler
{
    /// <summary>
    /// Processa a consulta de saldo consolidado.
    /// Retorna sempre decimal para evitar perda de precisão financeira.
    /// </summary>
    /// <param name="query">Os parâmetros da consulta (ex: a data).</param>
    /// <returns>O valor total do saldo consolidado.</returns>
    Task<decimal> Handle(GetBalanceQuery query);
}