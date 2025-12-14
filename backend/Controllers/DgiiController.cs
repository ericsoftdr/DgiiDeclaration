using DgiiIntegration.Common.Enums;
using DgiiIntegration.Models;
using DgiiIntegration.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DgiiIntegration.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DgiiController : ControllerBase
    {
        private DgiiService _dgiiService;
        private readonly ApplicationDbContext _dbContest;

        public DgiiController(DgiiService dgiiService, ApplicationDbContext dbContest)
        {
            _dgiiService = dgiiService;
            _dbContest = dbContest;
        }

        [HttpPost("/DeclarationInZero/AllCompanies")]
        public async Task<IActionResult> DeclarationInZeroAllCompaniesAsync()
        {
            try
            {
                var firstDayOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var companies = _dbContest.CompanyCredentials
                                           .Where(x => x.StatusInd 
                                                    && x.SelectedForProcessing 
                                                    && (x.DateProcessed == null || x.DateProcessed < firstDayOfCurrentMonth))
                                           .OrderBy(x => x.Id)
                                           .ToList();

                if (!companies.Any())
                    return BadRequest("No se encontraron compañias parametrizadas para ser procesadas.");

                //var rncArray = companies.Select(c => c.Rnc).ToArray();
                //var result = await _dgiiService.DeclarationInZeroListAsync(rncArray);
                var result = await _dgiiService.DeclarationInZeroAllCompaniesAsync();

                if (result.Any(r => !r.Status.Contains("Completado Satisfactoriamente")))
                {
                    return BadRequest(result.Where(x => !x.Status.Contains("Completado Satisfactoriamente"))
                                            .Select(e => new { e.Rnc, e.CompanyName, e.Status }));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("/DeclarationInZero/Companies")]
        public async Task<IActionResult> DeclarationInZeroSpecificCompaniesAsync([FromBody] string[] rncList)
        {
            try
            {
                if (rncList == null || !rncList.Any())
                    return BadRequest("No se encontraron compañias en el cuerpo de la solicitud.");

                var result = await _dgiiService.DeclarationInZeroListAsync(rncList);

                if (result.Any(r => !r.Status.Contains("Completado Satisfactoriamente")))
                {
                    return BadRequest(result.Where(x => !x.Status.Contains("Completado Satisfactoriamente"))
                                            .Select(e => new { e.Rnc, e.CompanyName, e.Status }));
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("/DeclarationInZero/{rnc}")]
        public async Task<IActionResult> DeclarationInZeroByRncAsync(string rnc)
        {
            try
            {
                var company = _dbContest.CompanyCredentials.FirstOrDefault(x => x.Rnc == rnc);

                if (company == null)
                    throw new Exception("Rnc no se encuentra creado en la aplicacion.");

                await _dgiiService.DeclarationInZeroAsync(company.Rnc, FormDeclaration.All);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("/DeclarationInZero")]
        public async Task<IActionResult> DeclarationInZeroAsync(string rnc, string formDeclaration)
        {
            try
            {
                formDeclaration = formDeclaration.Trim();

                FormDeclaration form;

                if (!Enum.TryParse(formDeclaration, out form))
                    throw new Exception("Codigo de formulario invalido");

                var company = _dbContest.CompanyCredentials.FirstOrDefault(x => x.Rnc == rnc);
                if (company == null)
                    throw new Exception("Rnc no se encuentra creado en la aplicacion.");

                await _dgiiService.DeclarationInZeroAsync(company.Rnc, form);

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("/SendEmail")]
        public async Task<IActionResult> SendEmail(string subject, List<(string Rnc, string CompanyName, string Status)> data)
        {
            try
            {
                data = new List<(string Rnc, string CompanyName, string Status)>
                {
                    ("131204783","ERICSOFT SRL","Estatus de prueba.")
                };
                
                
                await _dgiiService.SendEmail(subject, data);
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
