namespace Desafio_BT.Services;

public interface ITwelveDataService
{
    Task<decimal> GetCurrentPriceAsync(string symbol);
}