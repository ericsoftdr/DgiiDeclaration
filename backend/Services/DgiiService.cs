using DgiiIntegration.Common;
using DgiiIntegration.Common.Enums;
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
using ClosedXML.Excel;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using EricSoft.DgiiWebScraper;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DgiiIntegration.Services
{
    public class DgiiService
    {
        private IBrowser _browser;
        private IPage _page;
        private readonly AppSettings _appSettings;
        private Dictionary<int, string> _dgiiToken;
        private readonly ApplicationDbContext _dbContext;
        private CompanyCredential _company;
        private readonly ILogger<DgiiService> _logger;
        public DgiiService(IOptions<AppSettings> appSettings, ApplicationDbContext dbContext, ILogger<DgiiService> logger)
        {
            _appSettings = appSettings.Value;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task DeclarationInZeroAsync(string rnc, FormDeclaration specificformDeclaration, int startingPeriod = 0)
        {
            try
            {
                if (startingPeriod != 0 && startingPeriod < 200501)
                    throw new Exception("Indique un periodo de inicio mayor o igual a 200501");

                _logger.LogInformation($"========================================================================================");
                _logger.LogInformation($"Inicia el proceso para Rnc: {rnc}");
                _logger.LogInformation("inicializar el browser");
                await InitializeBrowserAsync();

                _logger.LogInformation("Busca los datos del RNC en la BD");
                await InitializeCompanyDataAsync(rnc);

                _logger.LogInformation("Hace login");
                await LoginAsync(_company.Rnc, _company.Pwd);

                //_logger.LogInformation("Valida si hay mensajes por leer o no");
                //var menuElementTask = _page.WaitForSelectorAsync("[id='2203']", new WaitForSelectorOptions { Timeout = 60000});
                //var frameMessageTask = _page.WaitForFrameAsync(frame => frame.Url.Contains("/OFV/AvisoMensajes.aspx"), new WaitForOptions { Timeout = 60000 });
                //var completedTask = await Task.WhenAny(menuElementTask, frameMessageTask);

                //if (completedTask == frameMessageTask && frameMessageTask.IsCompletedSuccessfully)
                //    throw new Exception("No se puede actualizar la sociedad debido a que tiene mensajes por leer.");

                //if (!menuElementTask.IsCompletedSuccessfully && !frameMessageTask.IsCompletedSuccessfully)
                //    throw new Exception("No se encontro una opcion del menu que compueba que el home cargó, pero tampoco se encontró el iframe que indica si hay mensajes para leer");

                //_logger.LogInformation("No hay mensajes por leer.");

                var isThereMessageToRead = await IsThereMessageToReadAsync(15000);

                if (isThereMessageToRead)
                    throw new Exception("No se puede actualizar la sociedad debido a que tiene mensajes por leer.");

                _logger.LogInformation("Busca el nombre de la compania (Razon Social) en la pagina");
                _company.CompanyName = await GetCompanyName();
                _logger.LogInformation($"El nombre de la compania es {_company.CompanyName}");

                int lastPeriodDeclared = 0;
                int nextPeriod = 0;

                //ITBIS
                if (specificformDeclaration != FormDeclaration.IR3)
                {
                    _logger.LogInformation("Determina ultimo periodo declarado.");
                    lastPeriodDeclared = await GetLastFormDeclarationAsync("IT1");
                    nextPeriod = startingPeriod != 0 ? startingPeriod : await GetNextPeriod(lastPeriodDeclared);
                    //nextPeriod = 201506;
                    if (!PeriodShouldBeDeclared(nextPeriod))
                        _logger.LogInformation($"La declaracion de ITBIS ya se encuentra actualizada.");

                    while (PeriodShouldBeDeclared(nextPeriod))
                    {
                        if (specificformDeclaration == FormDeclaration.All)
                        {
                            _logger.LogInformation($"Delcaracion 606, periodo {nextPeriod}");
                            await ZeroDeclaration(nextPeriod, "606");
                            _logger.LogInformation($"Delcaracion 607, periodo {nextPeriod}");
                            await ZeroDeclaration(nextPeriod, "607");
                            
                            if (nextPeriod >= 201801)
                            {
                                _logger.LogInformation($"Delcaracion Anexo IT1, periodo {nextPeriod}");
                                await InteractiveDeclaration("IT1", nextPeriod, anexo: true);
                            }
                            
                            _logger.LogInformation($"Delcaracion IT1, periodo {nextPeriod}");
                            await InteractiveDeclaration("IT1", nextPeriod);
                            nextPeriod = await GetNextPeriod(nextPeriod);
                            await Task.Delay(1000);

                            //if (nextPeriod >= 201601)
                            //    nextPeriod = 202509;
                        }
                        else
                        {
                            switch (specificformDeclaration)
                            {
                                case FormDeclaration.Form606:
                                    _logger.LogInformation($"Inicia Delcaracion 606 individual, periodo {nextPeriod}");
                                    await ZeroDeclaration(nextPeriod, "606");
                                    _logger.LogInformation($"FInaliza Delcaracion 606 individual, periodo {nextPeriod}");
                                    break;
                                case FormDeclaration.Form607:
                                    _logger.LogInformation($"Inicia Delcaracion 607 individual, periodo {nextPeriod}");
                                    await ZeroDeclaration(nextPeriod, "607");
                                    _logger.LogInformation($"Finaliza Delcaracion 607 individual, periodo {nextPeriod}");
                                    break;
                                case FormDeclaration.IT1A:
                                    _logger.LogInformation($"Inicia Delcaracion Anexo IT1 individual, periodo {nextPeriod}");
                                    await InteractiveDeclaration("IT1", nextPeriod, anexo: true);
                                    _logger.LogInformation($"Finaliza Delcaracion Anexo IT1 individual, periodo {nextPeriod}");
                                    break;
                                case FormDeclaration.IT1:
                                    _logger.LogInformation($"Inicia Delcaracion IT1 individual, periodo {nextPeriod}");
                                    await InteractiveDeclaration("IT1", nextPeriod);
                                    _logger.LogInformation($"Finaliza Delcaracion IT1 individual, periodo {nextPeriod}");
                                    break;
                            }

                            break;
                        }
                    }
                }


                //IR3
                _logger.LogInformation($"Se concluyo la parte de claracion de ITBIS. La especificacion de formulario a declara es: ''{specificformDeclaration.ToString()}''. Esto se evalua antes de iniciar el proceso de IR3.");

                if (specificformDeclaration == FormDeclaration.All || specificformDeclaration == FormDeclaration.IR3)
                {
                    lastPeriodDeclared = 0;
                    nextPeriod = 0;

                    _logger.LogInformation("Determina si requiere declaracion de IR3.");

                    var isFormDeclarationRequired = await IsFormDeclarationRequiredAsync("IR3");

                    _logger.LogInformation($"Declaración de IR3 {(isFormDeclarationRequired ? "requerida" : "NO requerida")}.");

                    if (isFormDeclarationRequired)
                    {
                        _logger.LogInformation("Determina si el último periodo declarado.");
                        lastPeriodDeclared = await GetLastFormDeclarationAsync("IR3");
                        _logger.LogInformation($"El ultimo periodo declara fue {lastPeriodDeclared}");

                        if (lastPeriodDeclared != 0)
                        {
                            nextPeriod = await GetNextPeriod(lastPeriodDeclared);

                            if (!PeriodShouldBeDeclared(nextPeriod))
                                _logger.LogInformation($"La declaracion de IR3 ya se encuentra actualizada.");

                            while (PeriodShouldBeDeclared(nextPeriod))
                            {
                                _logger.LogInformation($"Delcaracion IR3, periodo {nextPeriod}");
                                await ZeroDeclaration(nextPeriod, "IR3");
                                nextPeriod = await GetNextPeriod(nextPeriod);

                                if (specificformDeclaration == FormDeclaration.IR3) // Si se especifica es porque se requiere ejecutar para un solo periodo
                                {
                                    _logger.LogInformation($"FInaliza Delcaracion IR3 Individual, periodo {nextPeriod}");
                                    break;
                                }

                                await Task.Delay(1000);
                            }
                        }
                    }
                }

                _company.StatusInd = true;
                _company.DateProcessed = DateTime.Now;
                _dbContext.Update(_company);
                await _dbContext.SaveChangesAsync();

                //Cierra sesion
                await LogOff();

            }
            catch (Exception ex)
            {
                _logger.LogError($"Se produjo el siguiente error: {ex.Message}");
                //await _page.ScreenshotAsync($"fallo.png");
                //var html = await _page.GetContentAsync();
                //File.WriteAllText($"fallo.html", html);
                throw;
            }
            finally
            {
                _logger.LogInformation($"Cierra la pagina y el browser");

                if (_page != null)
                {
                    await _page.CloseAsync();
                    await _page.DisposeAsync();
                }

                if (_browser != null)
                {
                    await _browser.CloseAsync();
                    await _browser.DisposeAsync();
                }

                _logger.LogInformation($"pagina y browser Cerrado");
            }
        }

        public async Task<List<(string Rnc, string CompanyName, string Status)>> DeclarationInZeroListAsync(string[] rncList)
        {
            var result = new List<(string Rnc, string CompanyName, string Status)>();

            var companies = await _dbContext.CompanyCredentials
                                            .Where(c => rncList.Contains(c.Rnc))
                                            .Distinct() 
                                            .ToListAsync();

            foreach (var company in companies)
            {
                try
                {
                    await DeclarationInZeroAsync(company.Rnc, FormDeclaration.All);
                    result.Add((company.Rnc, company.CompanyName, "Completado Satisfactoriamente"));
                }
                catch (Exception ex)
                {
                    result.Add((company.Rnc, company.CompanyName, ex.Message));
                }
            }

            await SendEmail(result.Any(r => !r.Status.Contains("Completado Satisfactoriamente"))
                ? "Proceso DGII finalizado con error"
                : "Proceso DGII finalizado satisfactoriamente", result);

            if (result.Any(r => !r.Status.Contains("Completado Satisfactoriamente")))
            {
                throw new Exception("Proceso finalizado con error");
            }

            return result;
        }

        public async Task<List<(string Rnc, string CompanyName, string Status)>> DeclarationInZeroAllCompaniesAsync()
        {
            //Metodo provisional hasta que se actualice la vercion de la base de datos.
            var result = new List<(string Rnc, string CompanyName, string Status)>();
            var firstDayOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var companies = await _dbContext.CompanyCredentials
                                       .Where(x => x.StatusInd
                                                && x.SelectedForProcessing
                                                && (x.DateProcessed == null || x.DateProcessed < firstDayOfCurrentMonth))
                                       .OrderBy(x => x.Id)
                                       .ToListAsync();

            foreach (var company in companies)
            {
                try
                {
                    await DeclarationInZeroAsync(company.Rnc, FormDeclaration.All);
                    result.Add((company.Rnc, company.CompanyName, "Completado Satisfactoriamente"));
                }
                catch (Exception ex)
                {
                    result.Add((company.Rnc, company.CompanyName, ex.Message));
                }
            }

            //Genera excel con el resultado de la corrida***********************
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Data");

            worksheet.Cell("A1").Value = "RNC";
            worksheet.Cell("B1").Value = "Compañía";
            worksheet.Cell("C1").Value = "Mensaje";
            
            int row = 2;
            
            foreach(var data in result)
            {
                worksheet.Cell($"A{row}").SetValue(data.Rnc);
                worksheet.Cell($"B{row}").SetValue(data.CompanyName);
                worksheet.Cell($"C{row}").SetValue(data.Status);
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
               
                var content = stream.ToArray();
                string filePath = AppDomain.CurrentDomain.BaseDirectory + $"{DateTime.Now.ToString("yyyyMMdd hhmmss")}.xlsx";
               
                File.WriteAllBytes(filePath, content);
            }

            /******************************************************************/

            await SendEmail(result.Any(r => !r.Status.Contains("Completado Satisfactoriamente"))
                ? "Proceso DGII finalizado con error"
                : "Proceso DGII finalizado satisfactoriamente", result);

            if (result.Any(r => !r.Status.Contains("Completado Satisfactoriamente")))
            {
                throw new Exception("Proceso finalizado con error");
            }

            return result;
        }

        public async Task<RncCedula> GetRncAsync(string rnc)
        {
            var scraper = new DgiiWebScraperClient();
           return  await scraper.ConsultarRncCedulaAsync(rnc);
        }

        #region Metodos privados auxiliares
        private async Task InitializeBrowserAsync()
        {
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                ExecutablePath = _appSettings.ChromePath,
                Args = new[] { "--start-maximized", "--no-sandbox", "--disable-setuid-sandbox" }
            });

            _page = await _browser.NewPageAsync();
            _page.DefaultTimeout = 90000;
            _page.DefaultNavigationTimeout = 90000;

            await _page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });
        }
        private async Task InitializeCompanyDataAsync(string rnc)
        {
            _company = _dbContext.CompanyCredentials.Include(i => i.CompanyCredentialTokens)
                                                    .Include(i => i.AccountingManager)
                                                    .FirstOrDefault(x => x.Rnc == rnc);
            if (_company == null)
                throw new Exception("No se encontró una entidad para el RNC/Cedula suministrado.");

            _dgiiToken = _company.CompanyCredentialTokens.ToDictionary(x => x.TokenId, y => y.TokenValue);
        }
        private async Task LoginAsync(string username, string password)
        {
            var cts = new CancellationTokenSource();

            await _page.GoToAsync(_appSettings.DgiiUrlLogin, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });
            await _page.WaitForSelectorAsync("input[id*='txtUsuario']");
            await _page.TypeAsync("input[id*='txtUsuario']", username);
            await _page.TypeAsync("input[id*='txtPassword']", password);
            await _page.ClickAsync("input[id*='BtnAceptar']");

            string lblMensajeSelector = "span[id$='lblMensaje']";

            var errorMessageTask = _page.WaitForSelectorAsync(lblMensajeSelector, new WaitForSelectorOptions {
                Visible = true, 
                Timeout = 60000
            });

            var tokenRequiredTask = _page.WaitForSelectorAsync("input[id*='txtpasscodeTarjetaToken']", new WaitForSelectorOptions {
                Timeout = 60000
            });

            var navigationTask = WaitForUrlChangeAsync(_appSettings.DgiiUrlLogin, 60000, cts.Token);

            var completedTask = await Task.WhenAny(navigationTask, errorMessageTask, tokenRequiredTask);
            await cts.CancelAsync();

            if (completedTask == errorMessageTask && errorMessageTask.IsCompletedSuccessfully)
            {    
                var errorMessage = await _page.EvaluateFunctionAsync<string>(@"(selector) => {
                        const element = document.querySelector(selector);
                        return element ? element.innerText : null;
                    }", lblMensajeSelector);
                
                await cts.CancelAsync();

                throw new Exception($"Error de inicio de sesión. Detalle del error: {errorMessage}");
            }
            
            if (completedTask == tokenRequiredTask && tokenRequiredTask.IsCompletedSuccessfully)
            {
                if (!_company.TokenRequired)
                    throw new Exception("La compañía esta configurada sin doble factor de autenticación, sin embargo, la DGII esta solicitando tarjeta de código o softtoken para iniciar sesión");

                await _page.WaitForSelectorWithTimeoutAsync("[id*='txtpasscodeTarjetaToken']");
                var inputValue = await _page.EvaluateFunctionAsync<string>("() => document.querySelector('[id*=\"txtpasscodeTarjetaToken\"]').getAttribute('data-original-title')");

                Regex regex = new Regex(@"\d+");
                Match match = regex.Match(inputValue);

                if (match.Success)
                {
                    int idToken = Convert.ToInt16(match.Value);
                    _logger.LogInformation($"El id de la tarjeta de codigo que debe digitado para iniciar sesion es: {idToken}");
                    await _page.TypeAsync("[id*='txtpasscodeTarjetaToken']", _dgiiToken[idToken]);
                    await _page.ClickAsync("[id*='BtnAceptarTarjetaToken']");

                    var ctsToken = new CancellationTokenSource();

                    var navigationWithTokenTask = WaitForUrlChangeAsync(_appSettings.DgiiUrlLogin, 60000, ctsToken.Token);

                    var errorMessageWithTokenTask = _page.WaitForSelectorAsync(lblMensajeSelector, new WaitForSelectorOptions
                    {
                        Visible = true,
                        Timeout = 60000
                    });

                    var completedWithTokenTask = await Task.WhenAny(errorMessageWithTokenTask, navigationWithTokenTask);
                    
                    await ctsToken.CancelAsync();

                    _logger.LogInformation($"errorMessageWithTokenTask:{errorMessageWithTokenTask.IsCompletedSuccessfully}; navigationWithTokenTask:{navigationWithTokenTask.IsCompletedSuccessfully} ");
                    if (completedWithTokenTask == errorMessageWithTokenTask && errorMessageWithTokenTask.IsCompletedSuccessfully)
                    {
                        var errorMessageToken = await _page.EvaluateFunctionAsync<string>(@"(selector) => {
                            const element = document.querySelector(selector);
                            return element ? element.innerText : null;
                        }", lblMensajeSelector);

                        throw new Exception($"El código proporcionado para el id {idToken} de la tarjeta es erroneo. Detalle del error: {errorMessageToken}");
                    }
                     
                    if (!errorMessageWithTokenTask.IsCompletedSuccessfully && !navigationWithTokenTask.IsCompletedSuccessfully)
                    {
                        await ctsToken.CancelAsync();
                        throw new Exception($"Se colocó el codigo de la tarjeta, no marcó error pero tampoco se dirigió al home.");
                    }

                    await SetTokendAsValidatedAsync(idToken);
                }
                else
                {
                    throw new Exception("No se pudo localizar el token que deberia pedir la dgii");
                }
            }

            if (!navigationTask.IsCompletedSuccessfully && !errorMessageTask.IsCompletedSuccessfully 
                    && !tokenRequiredTask.IsCompletedSuccessfully)
                throw new Exception($"Error de inicio de sesión. La página no presentó un mensaje de error pero tampoco se dirigió al home.");
        }

        private async ValueTask<int> GetLastFormDeclarationAsync(string formulario)
        {
            int result = 0;

            await _page.ClickAsync("[id='2197'] ");

            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            await _page.WaitForSelectorWithTimeoutAsync("table[id$='dgResumenDoc']");

            var linkForm = await _page.EvaluateFunctionAsync<string>(@" (formulario) => {
                        const link = Array.from(document.querySelectorAll('a')).find(a => a.textContent.includes(formulario));
                        return link ? link.getAttribute('id') : null;
                    }", formulario);

            
            if (linkForm != null)
            {
                _logger.LogInformation($"Id del link del formulario {formulario}: {linkForm.ToString()}, esto es para dar click y buscar el ultimo período reportado.");

                await _page.ClickAsync($"#{linkForm}");
                
                await _page.WaitForNavigationAsync(new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                });

                await _page.WaitForSelectorWithTimeoutAsync("table[id$='dgDeclaraciones']");

                // Espera hasta que la tabla tenga filas cargadas
                await _page.WaitForFunctionAsync(@"() => {
                                  const table = document.querySelector('table[id$=""dgDeclaraciones""]');
                                  return table && table.querySelectorAll('tr').length > 1; // Asegúrate de que hay más de dos filas
                              }", new WaitForFunctionOptions { Timeout = 300000 });

                // Extraer los valores del tercer, cuarto y quinto <td> de la primera fila de datos
                //var rowData = await _page.EvaluateFunctionAsync<Dictionary<string, string>>(@"() => {
                //            const table = document.querySelector('table[id$=""dgDeclaraciones""]');
                //            const rows = table.querySelectorAll('tr');
                //            if (rows.length > 2) {
                //                const firstDataRow = rows[2]; // La primera fila de datos después del encabezado
                //                const cells = firstDataRow.querySelectorAll('td');
                //                return {
                //                    referencia: cells[2].innerText.trim(),
                //                    periodo: cells[3].innerText.trim(),
                //                    fecha: cells[4].innerText.trim()
                //                };
                //            }
                //            return null;
                //        }
                //    ");

                await Task.Delay(2000);

                var rowData = await _page.EvaluateFunctionAsync<Dictionary<string, string>>(@"() => {
                    const headerRow = document.querySelector('tr.theader');
                    if (headerRow) {
                        const firstDataRow = headerRow.nextElementSibling;
                        if (firstDataRow) {
                            const cells = firstDataRow.querySelectorAll('td');
                            return {
                                numero: cells[2].innerText.trim(),
                                periodo: cells[3].innerText.trim(),
                                fechaPresentacion: cells[4].innerText.trim()
                            };
                        }
                    }
                    return null;
                }
                ");


                if (rowData != null)
                    result = Convert.ToInt32(rowData["periodo"]);
                else
                    _logger.LogWarning("No se encontraron datos en la tabla de declaraciones de ITBIS.");
            }
            else
            {
                //Console.WriteLine("Enlace no encontrado.");
                //Si no encuentra el formulario es porque nunca se ha presentado y hay que hacerlo desde cero.
                _logger.LogError($"No se encontró el id Id del link del formulario {formulario}.");
                result = 0;
            }

            return result;
        }
        private async ValueTask<int> GetNextPeriod(int period)
        {
            string nexPeriod = "";

            if (period == 0) 
            {
                /*
                 * Si no se ha realizado ninguna declaracion entonces se parte de la fecha de inicio de actividad
                 *de la empresa, con la excepcion de no reportar más de los ultimos 42 meses
                 */
                /*Se remueve la rega de los ultimos 42 periodos*/

                //var lowerDeadlineDate = DateTime.Now.AddMonths(-42);
                var companyStartDateActivity = await GetCompanyStartDateActivity();
                //var datePeriod = (companyStartDateActivity > lowerDeadlineDate) ? companyStartDateActivity : lowerDeadlineDate;
                var datePeriod = companyStartDateActivity;
                nexPeriod = new DateTime(datePeriod.Year, datePeriod.Month, 1).ToString("yyyyMM");
            }
            else
            {
                var yr = Convert.ToInt32(period.ToString().Substring(0, 4));
                var month = Convert.ToInt32(period.ToString().Substring(4, 2));
                nexPeriod = new DateTime(yr, month, 1).AddMonths(1).ToString("yyyyMM");
            }

            return Convert.ToInt32(nexPeriod);
        }
        private bool PeriodShouldBeDeclared(int period)
        {
            var yr = Convert.ToInt32(period.ToString().Substring(0, 4));
            var month = Convert.ToInt32(period.ToString().Substring(4, 2));
            var datePeriod = new DateTime(yr, month, 1);

            var datePeriodLimit = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            return datePeriod < datePeriodLimit;
           
        }
        private async Task ZeroDeclaration(int period, string declaration)
        {
            await _page.ClickAsync("[id='2187']");

            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            await _page.WaitForSelectorWithTimeoutAsync("select[id$='ddlImpuesto']");

            var selectId = await _page.EvaluateFunctionAsync<string>(@" () => {
                const select = document.querySelector('select[id$=""ddlImpuesto""]');
                return select ? select.id : null;
               }
            ");

            if (string.IsNullOrEmpty(selectId))
                throw new Exception("No se encontró un elemento <select> cuyo ID termine en 'ddlImpuesto'.");

            // Espera hasta que el <select> tenga opciones cargadas
            await _page.WaitForFunctionAsync($@"() => {{
                const select = document.querySelector('#{selectId}');
                return select && select.options.length > 1; // Asegúrate de que hay más de una opción
               }}", new WaitForFunctionOptions { Timeout = 300000 });

            // Selecciona el formato de declaracion del <select>
            await _page.SelectAsync($"#{selectId}", declaration);
            await _page.WaitForSelectorWithTimeoutAsync("input[id$='txtPeriodo']");
            await _page.TypeAsync("input[id$='txtPeriodo']", period.ToString());
            await _page.ClickAsync("[id*='btnPresentar']");
            await _page.WaitForSelectorWithTimeoutAsync("span[id$='lblMsg']");

            string msg = await _page.EvaluateFunctionAsync<string>(@"() => { const element = document.querySelector('span[id$=""lblMsg""]');
                                                                            return element ? element.innerText : null; 
                                                                          }");
            if (msg == null)
                _logger.LogError($"El mensaje de confirmacion de la declaracion en cero del forumario {declaration} en el periodo {period} no fue encontrado o no contiene texto.");

            if (msg.Contains("satisfactoriamente"))
                await SaveScreeninPDF(_page, _company.CompanyName ?? "", $"{period.ToString().Substring(0, 4)}-{period.ToString().Substring(4, 2)}-{declaration} ");
            else if (msg.Contains("formato presentado") || msg.Contains("se encuentra presentado"))
                _logger.LogWarning($"El periodo {period} ya se encuentra presentado para el {declaration}, el mensaje obtenido de la DGII fue: {msg}");
            else
                throw new Exception($"Hubo un error tratando de declarar el {declaration}, el mensaje obtenido fue: {msg}");
                
        }
        private async Task InteractiveDeclaration(string declaration, int period, bool anexo = false)
        {
            await _page.ClickAsync("[id='2189']");

            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            await _page.WaitForSelectorWithTimeoutAsync("select[id$='lstFormularios']");

            // Espera hasta que el <select> tenga opciones cargadas
            await _page.WaitForFunctionAsync(@"() => {
                const select = document.querySelector('select[id$=""lstFormularios""]');
                return select && select.options.length > 1; // Asegúrate de que hay más de una opción
            }", new WaitForFunctionOptions { Timeout = 300000 });

            await _page.SelectAsync("select[id$='lstFormularios']", declaration);

            // Simula el clic en la opción seleccionada
            await _page.EvaluateFunctionAsync($@"() => {{
                    const select = document.querySelector('select[id$=""lstFormularios""]');
                    const option = select.querySelector('option[value=""{declaration}""]');
                    if (option) {{
                        select.value = option.value;
                        select.dispatchEvent(new Event('change', {{ bubbles: true }}));
                        option.click();
                    }}
                 }}");
            
            await _page.WaitForSelectorWithTimeoutAsync("input[id$='txtAnnio']");
            await _page.WaitForSelectorWithTimeoutAsync("select[id$='ddlMes']");
            await _page.WaitForSelectorWithTimeoutAsync("input[id$='btnAceptar']");
            await _page.WaitForSelectorAsync("#divperiodo", new WaitForSelectorOptions { Visible = true, Timeout = 120000 });

            // Asegurarse de que el input esté enfocado antes de escribir
            await _page.FocusAsync("input[id$='txtAnnio']");
            await _page.TypeAsync("input[id$='txtAnnio']", period.ToString().Substring(0, 4));
            await _page.SelectAsync("select[id$='ddlMes']", period.ToString().Substring(4, 2));
            await _page.ClickAsync("[id$='btnAceptar']");
            await Task.Delay(3000);
            await _page.WaitForSelectorWithTimeoutAsync("table[id$='tblAnexos']");
            var lblLink = anexo ? "hkAnexo" : "LNKFORM";
            await _page.WaitForSelectorWithTimeoutAsync($"a[id$='{lblLink}']");
            await _page.ClickAsync($"a[id$='{lblLink}']");

            string tokenId = "";

            if (_company.TokenRequired)
            {
                // Espera a que el <span> esté disponible debajo del <div> con id que termina en 'pnlTarjeta'
                await _page.WaitForSelectorWithTimeoutAsync("div[id$='pnlTarjeta'] span.label.label-success");
                await _page.WaitForSelectorWithTimeoutAsync("input[id$='txtCodigoTarjeta']");
                await _page.WaitForSelectorWithTimeoutAsync("input[id$='btnTarjetaContinuar']");

                // Extrae el texto del <span> y obtén el número
                tokenId = await _page.EvaluateFunctionAsync<string>(@"() => {
                    const span = document.querySelector('div[id$=""pnlTarjeta""] span.label.label-success');
                    return span ? span.innerText : null;
                 }");

                if (tokenId != null)
                    _logger.LogInformation($"El id de la tarjeta de codigo que debe ser digitado es: {tokenId}");
                else
                    throw new Exception("El id de la tarjeta de código no localizado.");

                await _page.TypeAsync("input[id$='txtCodigoTarjeta']", _dgiiToken[Convert.ToInt32(tokenId)]);
                //await _page.TypeAsync("input[id$='txtCodigoTarjeta']", "1000");

                await _page.ClickAsync("input[id$='btnTarjetaContinuar']");
            }
         
            var cts = new CancellationTokenSource();
            string errorMessageSelector = "div[id$='pnlTarjeta'] label > span";
            var errorMessageTask = _page.WaitForSelectorAsync(errorMessageSelector, new WaitForSelectorOptions { Timeout = 60000 });
            var newPageTask = WaitForNewWindows(declaration + (anexo ? "A" : ""), 60000, cts.Token);
         
            var completedTask = await Task.WhenAny(errorMessageTask, newPageTask);
            await cts.CancelAsync();

            if (completedTask == errorMessageTask && errorMessageTask.IsCompletedSuccessfully)
            {
                var errorMessage = await _page.EvaluateFunctionAsync<string>(@"(selector) => {
                    const span = document.querySelector(selector);
                    return span ? span.innerText : null;
                 }"
                , errorMessageSelector);

                throw new Exception($"El código proporcionado para el id {tokenId} de la tarjeta es erroneo. Detalle del error: {errorMessage}");
            }
            
            if (!errorMessageTask.IsCompletedSuccessfully && !newPageTask.IsCompletedSuccessfully)
                throw new Exception($"No se detectó un mensaje de error para el token digitado, pero tampoco la ventana emergente de declaracion se mostró.");

            if (_company.TokenRequired)
                await SetTokendAsValidatedAsync(Convert.ToInt32(tokenId));

            var newPage = await newPageTask;
            newPage.DefaultTimeout = 300000;
            await newPage.WaitForSelectorWithTimeoutAsync("#divPrint");
            await newPage.WaitForSelectorWithTimeoutAsync("div[id='divPrint'] input[value='Cerrar']");
            await newPage.WaitForSelectorWithTimeoutAsync("input[id='btnEnviarD']");

            if (period >= 201801)
                await newPage.WaitForSelectorWithTimeoutAsync("#TableMain");

            await Task.Delay(5000);

            //Crea evento para aceptar los cuadros de dialogos
            EventHandler<DialogEventArgs> dialogHandler = null;

            dialogHandler = async (s, dialogEvent) =>
            {
                await dialogEvent.Dialog.Accept();
            };

            newPage.Dialog += dialogHandler;

            await newPage.ClickAsync("input[id='btnEnviarD']");
            await newPage.WaitForNetworkIdleAsync();
            
            var frame = await newPage.WaitForFrameAsync(
                   frame => frame. Url.Contains("/OFV/plantilla/Mensaje"),
                   new WaitForOptions { Timeout = 180000 }
               );

            //Asegurar de que el frame no sea nulo
            if (frame == null)
                _logger.LogError("No se pudo obtener el iframe.");

            // Espera a que el botón dentro del iframe esté disponible
            await frame.WaitForSelectorAsync("input[id='btnAceptar']", new WaitForSelectorOptions { Timeout = 300000 });

            // Hacer clic en el botón dentro del iframe
            await Task.Delay(1000);
            await frame.ClickAsync("input[id='btnAceptar']");
            await Task.Delay(2000);
            
            string formNameForFile = declaration == "IT1" ? "IT-1 ITBIS" : declaration;
            await SaveScreeninPDF(newPage, _company.CompanyName ?? "", $"{period.ToString().Substring(0, 4)}-{period.ToString().Substring(4, 2)} - {(anexo ? "Anexo A" : "")} {formNameForFile}");
            //_page.Browser.TargetCreated -= targetCreatedHandler;
            newPage.Dialog -= dialogHandler;
            await newPage.CloseAsync();
        }
        private async Task SaveScreeninPDF(IPage page, string companyName, string fileName)
        {
            string accountinManager = "UNASSIGNED";

            if (_company.AccountingManager != null && !string.IsNullOrWhiteSpace(_company.AccountingManager.BusinessName))
                accountinManager = _company.AccountingManager.BusinessName;

            //string baseDirectory = $"{_appSettings.PathPdfOfEvidence}\\{accountinManager}\\{companyName}";
            string baseDirectory = $"{_appSettings.PathPdfOfEvidence}\\{accountinManager}";

            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            var folders = Directory.EnumerateDirectories(baseDirectory, _company.Rnc.Trim() + "*");
            bool folderFound = false;

            if (folders.Any())
            {
                var folder = folders.FirstOrDefault(x => Path.GetFileName(x).Split('-')[0].Trim() == _company.Rnc.Trim());

                if (folder != null)
                {
                    baseDirectory = baseDirectory + "\\" + Path.GetFileName(folder);
                    folderFound = true;
                }
            }

            if (!folderFound)
                baseDirectory = baseDirectory + "\\" + _company.Rnc.Trim() + "-" + companyName;

            if (!Directory.Exists(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            var fullPath = $"{baseDirectory}\\{fileName} - {companyName.Trim()}.pdf";

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);


            await page.PdfAsync(fullPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = @"
                <div style='width: 100%; font-size: 10px; display: flex; justify-content: space-between; align-items: center; padding: 0 20px; box-sizing: border-box;'>
                    <div style='text-align: left; width: 33%;'>
                        <span class='date'></span>
                    </div>
                    <div style='text-align: center; width: 33%;'>
                        <span class='title'></span>
                    </div>
                    <div style='width: 33%;'></div>
                </div>",
                FooterTemplate = @"
                <div style='width: 100%; font-size: 10px; display: flex; justify-content: space-between; padding: 0 20px; box-sizing: border-box;'>
                    <div style='text-align: left; width: 33%;'>
                        <span class='url'></span>
                    </div>
                    <div style='text-align: right; width: 33%;'>
                        <span class='pageNumber'></span>/<span class='totalPages'></span>
                    </div>
                </div>",
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Bottom = "20px",
                    Left = "20px",
                    Right = "20px"
                }
            });
        }
        private async ValueTask<bool> IsFormDeclarationRequiredAsync(string formToCheck)
        {
            await GoToHome();
            await _page.WaitForSelectorWithTimeoutAsync("table[id$='dgObligaciones']");
            await Task.Delay(1000);

            bool rowExists = await _page.EvaluateFunctionAsync<bool>(@"(formToCheck) => 
            {
                const rows = document.querySelectorAll('table[id$=""dgObligaciones""] tr');
                for (const row of rows) {
                    const firstCell = row.querySelector('td');
                    if (firstCell && firstCell.innerText.trim() === formToCheck) {
                        return true;
                    }
                }
                return false;
            }", formToCheck);

            return rowExists;
        }
        private async Task GoToHome()
        {
            string divId = "logo";

            // Espera a que el div esté presente antes de buscar el enlace
            await _page.WaitForSelectorWithTimeoutAsync($"#{divId}");
           
            // Usa EvaluateFunctionAsync para encontrar el enlace y hacer clic en él
            await _page.EvaluateFunctionAsync(@"(divId) => {
                const div = document.getElementById(divId);
                if (div) {
                    const link = div.querySelector('a');
                    if (link) {
                        link.click();
                    }
                }
            }", divId);

            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });
        }
        private async ValueTask<DateTime> GetCompanyStartDateActivity()
        {
            string inputField = "txtInicioActividad";
            await _page.ClickAsync("[id='2203']");
            await _page.WaitForSelectorWithTimeoutAsync($"input[id$='{inputField}']");

            var dateFromPage = await _page.EvaluateFunctionAsync($@"(inputField) => {{
                const element = document.querySelector('input[id$=""{inputField}""]');
                return element ? element.value : null;
             }}", inputField);

            var companyStartDateActivity = new DateTime();

            if (!DateTime.TryParseExact(dateFromPage.ToString(), "yyyy/MM/dd", null, DateTimeStyles.None, out companyStartDateActivity))
                throw new Exception("La fecha de inicio de actividad de la sociedad no es valida.");
            
            return companyStartDateActivity;
        }

        private async ValueTask<string> GetCompanyName()
        {
            string inputField = _company.NaturalPerson ? "txtNombrePersona" : "txtRazonSocial";
            await _page.WaitForSelectorWithTimeoutAsync("[id='2203']");
            await _page.ClickAsync("[id='2203']");

            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            }).ConfigureAwait(false);

            await _page.WaitForSelectorWithTimeoutAsync($"input[id$='{inputField}']");

            var companyName = await _page.EvaluateFunctionAsync($@"(inputField) => {{
                const element = document.querySelector('input[id$=""{inputField}""]');
                return element ? element.value : null;
             }}", inputField);

            return companyName.ToString().Trim();
        }

        private async ValueTask<bool> IsThereMessageToReadAsync(int timeout)
        {
            bool result = false;

            try
            {
                // Esperar hasta que el iframe aparezca con un tiempo de espera específico
                _logger.LogInformation("Espera por el Iframe que indica si hay mensajes por leer");
                var frame = await _page.WaitForFrameAsync(
                    frame => frame.Url.Contains("/OFV/AvisoMensajes.aspx"),
                    new WaitForOptions { Timeout = timeout }
                );

                result = frame != null;
            }
            catch (TimeoutException ex)
            {
                _logger.LogInformation($"TimeoutException: El iframe no fue encontrado dentro del tiempo esperado. Detalles: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Manejo general de cualquier otra excepción
               throw new Exception($"Exception: Ocurrió un error al verificar el iframe. Detalles: {ex.Message}");
            }
            return result;
        }

        private async Task LogOff()
        {
            _logger.LogInformation($"Cierra sesion");

            await _page.EvaluateFunctionAsync(@"() => {
                const elements = document.querySelectorAll('a');
                for (let element of elements) {
                    if (element.innerText.includes('SALIR')) {
                        element.click();
                        break;
                    }
                }
            }");

            _logger.LogInformation($"Espera finaliza navegacion luego del cierre de sesion.");
            await _page.WaitForNavigationAsync(new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });
        }

        private async Task WaitForUrlChangeAsync(string initialUrl, int timeout, CancellationToken token)
        {
            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < timeout && _page != null)
            {
                token.ThrowIfCancellationRequested();

                var currentUrl = _page.Url;
                if (!string.Equals(initialUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                await Task.Delay(100); // Espera un pequeño intervalo antes de volver a verificar
            }

            throw new TimeoutException("Timeout waiting for URL to change");
        }

        private async Task<IPage> WaitForNewWindows(string declaration, int timeout, CancellationToken token)
        {
            IPage newPage = null;
            var waitLimitForNewPage = DateTime.Now.AddMilliseconds(timeout); 

            EventHandler<TargetChangedArgs> targetCreatedHandler = null;
            targetCreatedHandler = async (sender, e) =>
            {
                if (e.Target.Type == TargetType.Page)
                {
                    newPage = await e.Target.PageAsync();
                    _page.Browser.TargetCreated -= targetCreatedHandler;
                }
            };

            _page.Browser.TargetCreated += targetCreatedHandler;

            while (newPage == null)
            {
                token.ThrowIfCancellationRequested();

                if (DateTime.Now > waitLimitForNewPage)
                {
                    _page.Browser.TargetCreated -= targetCreatedHandler;
                    throw new Exception($"La ventana emergente {declaration} del no se mostró en el tiempo esperado.");
                }

                await Task.Delay(100, token);
            }

            return newPage;
        }

        public async Task SendEmail(string subject, List<(string Rnc, string CompanyName, string Status)> data)
        {
            string smtpServer = _appSettings.SmtpServer;
            int smtpPort = _appSettings.SmtpPort;
            string smtpUser = _appSettings.SmtpUser;
            string smtpPass = _appSettings.SmtpPass;

            string fromEmail = _appSettings.SmtpUser;
            string toEmail = _appSettings.ToEmail;
            string body = "";
            var htmDetail = "";

            if (data.Any())
            {
                foreach(var error in data.OrderBy(x => (x.Status == "Completado Satisfactoriamente" ? 1 : 0)).ToList())
                {
                    htmDetail = htmDetail + $@" <tr>
                                                    <td style='border: 1px solid; padding:2px 5px 2px 5px;'>{error.Rnc}</td>
                                                    <td style='border: 1px solid; padding:2px 5px 2px 5px;'>{error.CompanyName}</td>
                                                    <td style='border: 1px solid; padding:2px 5px 2px 5px;'>{error.Status}</td>
                                                </tr>";
                }

                body = @$" <p>Saludos,</p>
                            <p>Debajo resultado de la ejecucion del proceso</p>
                            <table style='border-collapse: collapse;'>
                                <thead>
                                    <tr>
                                        <th>RNC</th>
                                        <th>Razón Social</th>
                                        <th>Estatus</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {htmDetail}
                                </tbody>
                            </table>
                                <p>Nota: Correo enviado de forma automática, favor no responder a este Correo.</p> 
                                <p>Gracias.</p>";
            }

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromEmail);
                    mail.To.Add(toEmail);
                    mail.To.Add("nursandy@hotmail.com");
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true; 

                    using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
                        smtp.EnableSsl = true;

                        //await smtp.SendMailAsync(mail);
                        await smtp.SendMailAsync(mail);
                        _logger.LogInformation("Correo enviado exitosamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error al enviar el correo: {ex.Message}");
            }
        }
        private async Task SetTokendAsValidatedAsync(int tokenId)
        {
            var token = _dbContext.CompanyCredentialTokens
                                 .Single(x => x.CompanyCredential.Rnc == _company.Rnc && x.TokenId == tokenId);
            token.Validated = true;
            await _dbContext.SaveChangesAsync();
        }

        #endregion
    }
}
