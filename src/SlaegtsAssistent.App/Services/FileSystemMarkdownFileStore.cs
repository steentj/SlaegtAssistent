using System;
using System.IO;
using System.Text;

namespace SlaegtsAssistent.App.Services;

public sealed class FileSystemMarkdownFileStore : IMarkdownFileStore
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly IAtomicFileWriter _atomicFileWriter;

    public FileSystemMarkdownFileStore()
        : this(new AtomicFileWriter())
    {
    }

    public FileSystemMarkdownFileStore(IAtomicFileWriter atomicFileWriter)
    {
        _atomicFileWriter = atomicFileWriter ?? throw new ArgumentNullException(nameof(atomicFileWriter));
    }

    public string Read(string path)
    {
        return File.ReadAllText(path);
    }

    public void Write(string path, string content)
    {
        _atomicFileWriter.WriteText(path, content, Utf8WithoutBom);
    }
}
