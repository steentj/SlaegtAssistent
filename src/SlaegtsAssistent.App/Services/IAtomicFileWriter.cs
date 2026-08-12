using System.Text;

namespace SlaegtsAssistent.App.Services;

public interface IAtomicFileWriter
{
    void WriteText(string path, string content, Encoding encoding);

    void WriteBytes(string path, byte[] content);
}
