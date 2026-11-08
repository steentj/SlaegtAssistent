using System;
using System.IO;
using System.Text;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed class TemplateEditorStorageService
{
    private readonly IAtomicFileWriter _writer;

    public TemplateEditorStorageService(IAtomicFileWriter? writer = null) =>
        _writer = writer ?? new AtomicFileWriter();

    public BiographyTemplateEditorModel Load(string path) =>
        new BiographyTemplateLoader().LoadEditorModel(path);

    public void Save(string path, BiographyTemplateEditorModel model)
    {
        var serializer = new BiographyTemplateEditorSerializer();
        _ = new BiographyTemplateEditorCompiler().Compile(model);
        var content = serializer.Serialize(model);
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        var stagedPath = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            _writer.WriteText(stagedPath, content, new UTF8Encoding(false));
            var restored = Load(stagedPath);
            if (!string.Equals(content, serializer.Serialize(restored), StringComparison.Ordinal))
            {
                throw new IOException("Den genindlæste skabelon svarer ikke til den gemte kladde.");
            }

            File.Move(stagedPath, destination, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(stagedPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
