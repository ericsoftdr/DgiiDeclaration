using AccountingManagerApi.Services;
using DgiiIntegration.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccountingManagerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountingManagerController : ControllerBase
    {
        private readonly AccountingManagerService _service;

        public AccountingManagerController(AccountingManagerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<AccountingManager>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AccountingManager>> GetById(int id)
        {
            var manager = await _service.GetByIdAsync(id);
            return manager != null ? Ok(manager) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Create(AccountingManager manager)
        {
            await _service.CreateAsync(manager);
            return CreatedAtAction(nameof(GetById), new { id = manager.Id }, manager);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, AccountingManager manager)
        {
            if (id != manager.Id) return BadRequest();

            await _service.UpdateAsync(manager);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
