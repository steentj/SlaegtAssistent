using System.IO;
using System.Text;

namespace SlaegtsAssistent.App.Services;

public sealed class FileSystemMarkdownFileStore : IMarkdownFileStore
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    public string Read(string path)
    {
        return File.ReadAllText(path);
    }

    public void Write(string path, string content)
    {
        File.WriteAllText(path, content, Utf8WithoutBom);
    }
}
