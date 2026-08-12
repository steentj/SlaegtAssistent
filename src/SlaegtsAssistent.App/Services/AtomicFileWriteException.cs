using System;
using System.IO;

namespace SlaegtsAssistent.App.Services;

public sealed class AtomicFileWriteException : IOException
{
    public AtomicFileWriteException(string path, Exception innerException)
        : base(
            $"Filen '{path}' kunne ikke gemmes sikkert. Den tidligere version er bevaret, " +
            "hvis den fandtes. Kontrollér adgang og ledig diskplads, og prøv igen.",
            innerException)
    {
        Path = path;
    }

    public string Path { get; }
}
