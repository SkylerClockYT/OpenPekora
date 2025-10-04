using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiExplorerGetsOnlyConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        var apiExplorerSettings = action.Controller.Attributes.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
        if (apiExplorerSettings != null)
        {
            bool isVisible = (!apiExplorerSettings.IgnoreApi || apiExplorerSettings.GroupName != null) && (action.Attributes.OfType<HttpGetAttribute>().Any() || action.Attributes.OfType<HttpPostAttribute>().Any());
            action.ApiExplorer.IsVisible = isVisible;
            return;
        }
        action.ApiExplorer.IsVisible = false;
    }
}
