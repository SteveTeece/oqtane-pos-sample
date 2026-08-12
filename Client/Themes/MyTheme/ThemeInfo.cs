using System.Collections.Generic;
using Oqtane.Models;
using Oqtane.Themes;
using Oqtane.Shared;

namespace Yosh.uwa.MyTheme
{
    public class ThemeInfo : ITheme
    {
        public Oqtane.Models.Theme Theme => new Oqtane.Models.Theme
        {
            Name = "MyTheme",
            Version = "1.0.0",
            PackageName = "Yosh.uwa",
            ThemeSettingsType = "Yosh.uwa.MyTheme.ThemeSettings, Yosh.uwa.Client.Oqtane",
            ContainerSettingsType = "Yosh.uwa.MyTheme.ContainerSettings, Yosh.uwa.Client.Oqtane",
            Resources = new List<Resource>()
            {
                new Stylesheet(Constants.BootstrapStylesheetUrl, Constants.BootstrapStylesheetIntegrity, "anonymous"),
                new Stylesheet("~/Theme.css"),
                new Script(Constants.BootstrapScriptUrl, Constants.BootstrapScriptIntegrity, "anonymous")
            }
        };
    }
}
