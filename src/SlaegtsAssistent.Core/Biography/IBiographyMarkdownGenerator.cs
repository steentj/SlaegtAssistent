using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public interface IBiographyMarkdownGenerator
{
    string Generate(Person person);
}
