using DgiiIntegration.Common.Enums;
using DgiiIntegration.DTOs;
using DgiiIntegration.Models;
using DgiiIntegration.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DgiiIntegration.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompaniesController : ControllerBase
    {
        private CompaniesService _companiesService;
        private readonly ApplicationDbContext _dbContest;

        public CompaniesController(CompaniesService companiesService, ApplicationDbContext dbContest)
        {
            _companiesService = companiesService;
            _dbContest = dbContest;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompanyCredential>>> GetCompanyCredentials()
        {
            var companyCredentials = await _companiesService.GetAllAsync();
            return Ok(companyCredentials);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyCredential>> GetCompanyCredential(int id)
        {
            var companyCredential = await _companiesService.GetByIdAsync(id);
            if (companyCredential == null)
            {
                return NotFound();
            }
            return Ok(companyCredential);
        }


        [HttpPost]
        public async Task<ActionResult<CompanyCredential>> PostCompanyCredential([FromBody] CompanyCredentialCreateDto dto)
        {
            var createdCompanyCredential = await _companiesService.CreateFromDtoAsync(dto);
            return CreatedAtAction(nameof(GetCompanyCredential), new { id = createdCompanyCredential.Id }, createdCompanyCredential);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCompanyCredential(int id, CompanyCredential companyCredential)
        {
            try
            {
                await _companiesService.UpdateAsync(id, companyCredential);
                return NoContent();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompanyCredential(int id)
        {
            try
            {
                await _companiesService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
