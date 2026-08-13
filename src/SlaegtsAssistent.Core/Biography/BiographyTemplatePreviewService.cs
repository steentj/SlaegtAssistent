using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplatePreviewService
{
    public string Render(
        string templatePath,
        Person person,
        Submitter? submitter = null,
        string? mediaBaseDirectory = null,
        string? gedcomSourceDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
        {
            throw new ArgumentException("Skabelonfilen er påkrævet.", nameof(templatePath));
        }

        ArgumentNullException.ThrowIfNull(person);

        var template = new BiographyTemplateLoader().Load(templatePath);
        var context = BiographyTemplateContext.FromPerson(
            person,
            submitter,
            mediaBaseDirectory,
            gedcomSourceDirectory);
        return new BiographyTemplateRenderer().Render(template, context);
    }
}
