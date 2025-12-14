using PuppeteerSharp;
using System.Linq;

namespace DgiiIntegration.Common
{
    public static class PuppeteerExtensions
    {
        private const int GlobalTimeout = 300000; // 5 minutos en milisegundos

        public static async Task<ElementHandle> WaitForSelectorWithTimeoutAsync(this IPage page, string selector)
        {
            return (ElementHandle)await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
            {
                Timeout = GlobalTimeout
            });
        }
    }
}
