using DgiiIntegration.Common;
using DgiiIntegration.Common.Enums;
using DgiiIntegration.DTOs;
using DgiiIntegration.Helpers;
using DgiiIntegration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Globalization;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DgiiIntegration.Services
{
    public class CompaniesService
    {
        private IBrowser _browser;
        private IPage _page;
        private readonly AppSettings _appSettings;
        private Dictionary<int, string> _dgiiToken;
        private readonly ApplicationDbContext _dbContext;
        private CompanyCredential _company;
        private readonly ILogger<DgiiService> _logger;
        public CompaniesService(IOptions<AppSettings> appSettings, ApplicationDbContext dbContext, ILogger<DgiiService> logger)
        {
            _appSettings = appSettings.Value;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<CompanyCredential>> GetAllAsync()
        {
            return await _dbContext.CompanyCredentials
                .Include(c => c.AccountingManager)
                //.Include(c => c.CompanyCredentialTokens)
                .ToListAsync();
        }

        public async Task<CompanyCredential> GetByIdAsync(int id)
        {
            return await _dbContext.CompanyCredentials
                                   .Include(c => c.AccountingManager)
                                   .Include(c => c.CompanyCredentialTokens)
                                   .Where(x => x.Id == id)
                                   .FirstOrDefaultAsync();
        }

        public async Task<CompanyCredential> CreateAsync(CompanyCredential companyCredential)
        {
            try
            {
                if (!string.IsNullOrEmpty(companyCredential.TokenFileBase64))
                {
                    companyCredential.TokenFile = Convert.FromBase64String(companyCredential.TokenFileBase64);
                }
                _dbContext.CompanyCredentials.Add(companyCredential);
                await _dbContext.SaveChangesAsync();
                return companyCredential;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inesperado al crear Compañia", ex);
            }
        }

        public async Task<CompanyCredential> CreateFromDtoAsync(CompanyCredentialCreateDto dto)
        {
            try
            {
                var companyCredential = new CompanyCredential
                {
                    Rnc = dto.Rnc,
                    CompanyName = dto.CompanyName,
                    Pwd = dto.Pwd,
                    TokenRequired = dto.TokenRequired,
                    StatusInd = dto.StatusInd,
                    SelectedForProcessing = dto.SelectedForProcessing,
                    AccountingManagerId = dto.AccountingManagerId,
                    FileType = dto.FileType,
                    CompanyCredentialTokens = new List<CompanyCredentialToken>()
                };

                if (!string.IsNullOrEmpty(dto.TokenFileBase64))
                {
                    companyCredential.TokenFile = Convert.FromBase64String(dto.TokenFileBase64);
                }

                // Mapear los tokens del DTO a entidades
                foreach (var tokenDto in dto.CompanyCredentialTokens)
                {
                    companyCredential.CompanyCredentialTokens.Add(new CompanyCredentialToken
                    {
                        TokenId = tokenDto.TokenId,
                        TokenValue = tokenDto.TokenValue,
                        Validated = tokenDto.Validated
                    });
                }

                _dbContext.CompanyCredentials.Add(companyCredential);
                await _dbContext.SaveChangesAsync();
                return companyCredential;
            }
            catch (Exception ex)
            {
                throw new Exception("Error inesperado al crear Compañia", ex);
            }
        }

        public async Task UpdateAsync(int id, CompanyCredential companyCredential)
        {
            if (id != companyCredential.Id)
            {
                throw new Exception("Compañia no encontrada");
            }

            _dbContext.Entry(companyCredential).State = EntityState.Modified;

            var existingTokens = await _dbContext.CompanyCredentialTokens
                .Where(t => t.CompanyCredentialId == id)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                if (!companyCredential.CompanyCredentialTokens.Any(t => t.Id == token.Id))
                {
                    _dbContext.CompanyCredentialTokens.Remove(token);
                }
            }
            foreach (var token in companyCredential.CompanyCredentialTokens)
            {
                if (token.Id == 0) // nuevo token
                {
                    _dbContext.CompanyCredentialTokens.Add(token);
                }
                else
                {
                    var existingToken = await _dbContext.CompanyCredentialTokens.FindAsync(token.Id);
                    if (existingToken != null)
                    {
                        _dbContext.Entry(existingToken).CurrentValues.SetValues(token);
                    }
                    else
                    {
                        _dbContext.CompanyCredentialTokens.Update(token);
                    }
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                if (!await CompanyCredentialExists(id))
                {
                    throw new Exception("Compañia no encontrada");
                }
                throw;
            }
        }


        public async Task DeleteAsync(int id)
        {
            var companyCredential = await _dbContext.CompanyCredentials.FindAsync(id);
            if (companyCredential == null)
            {
                throw new Exception("Compañia no encontrada");
            }

            _dbContext.CompanyCredentials.Remove(companyCredential);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<bool> CompanyCredentialExists(int id)
        {
            return await _dbContext.CompanyCredentials.AnyAsync(e => e.Id == id);
        }
    }
}
