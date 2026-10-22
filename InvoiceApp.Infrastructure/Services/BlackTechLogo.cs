using System.Reflection;

namespace InvoiceApp.Infrastructure.Services;

public static class BlackTechLogo
{
    private const string ResourceName = "InvoiceApp.Infrastructure.Branding.blacktech-empower-logo.svg";

    public static string SvgMarkup
    {
        get
        {
            using var stream = typeof(BlackTechLogo).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException("The BlackTech logo resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
