using Microsoft.AspNetCore.Mvc;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Controllers
{
    /// <summary>
    /// Controller for handling Kline data operations.
    /// </summary>
    /// <param name="repository">The Kline data repository.</param>
    [ApiController]
    [Route("api/[controller]")]
    public class KlineDataController(IKlineDataRepository repository) : ControllerBase
    {
        private readonly IKlineDataRepository _repository = repository;

        /// <summary>
        /// Inserts new Kline data into the database.
        /// </summary>
        /// <param name="klineData">The KlineData object to insert.</param>
        /// <returns>A status indicating the result of the operation.</returns>
        [HttpPost("insert")]
        public async Task<IActionResult> InsertKlineData([FromBody] KlineData klineData)
        {
            await _repository.InsertKlineData(klineData);
            return Ok("Kline data inserted successfully.");
        }
    }
}
