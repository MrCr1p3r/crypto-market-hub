using SVC_Kline.Models.Input;

namespace SVC_Kline.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to Kline data.
/// </summary>
public interface IKlineDataRepository
{
    /// <summary>
    /// Inserts a new Kline data entry into the database.
    /// </summary>
    /// <param name="klineData">The KlineData object to insert.</param>
    public Task InsertKlineData(KlineData klineData);
}
