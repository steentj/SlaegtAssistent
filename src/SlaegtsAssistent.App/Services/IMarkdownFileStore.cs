namespace SlaegtsAssistent.App.Services;

public interface IMarkdownFileStore
{
    string Read(string path);

    void Write(string path, string content);
}
