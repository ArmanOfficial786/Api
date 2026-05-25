using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NexgenCosysReport.Inteface.ReportInterface;
namespace NexgenCosysReport.Services.ReportService
{
    public class RazorRenderService : IRazorRenderService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RazorRenderService> _logger;

        public RazorRenderService(IServiceProvider serviceProvider, ILogger<RazorRenderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<string> RenderToStringAsync(string viewName, object model)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sp = scope.ServiceProvider;
                var httpContext = new DefaultHttpContext { RequestServices = sp };
                var actionCtx = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
                var viewEngine = sp.GetRequiredService<IRazorViewEngine>();
                var tempProvider = sp.GetRequiredService<ITempDataProvider>();

                var viewResult = viewEngine.GetView(null, viewName, false);
                if (!viewResult.Success)
                    viewResult = viewEngine.FindView(actionCtx, viewName, false);
                if (!viewResult.Success)
                    throw new InvalidOperationException($"View '{viewName}' not found.");

                await using var sw = new StringWriter();

                var viewData = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(), new ModelStateDictionary())
                { Model = model };

                var viewContext = new ViewContext(
                    actionCtx, viewResult.View, viewData,
                    new TempDataDictionary(httpContext, tempProvider),
                    sw, new HtmlHelperOptions());

                await viewResult.View.RenderAsync(viewContext).ConfigureAwait(false);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RazorRenderService failed for view '{View}'", viewName);
                throw;
            }
        }
    }
}
