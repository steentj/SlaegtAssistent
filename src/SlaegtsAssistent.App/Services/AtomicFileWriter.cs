using System;
using System.IO;
using System.Text;

namespace SlaegtsAssistent.App.Services;

public sealed class AtomicFileWriter : IAtomicFileWriter
{
    private const int BufferSize = 81920;
    private readonly Action<AtomicWriteStage>? _stageCallback;

    public AtomicFileWriter(Action<AtomicWriteStage>? stageCallback = null)
    {
        _stageCallback = stageCallback;
    }

    public void WriteText(string path, string content, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(encoding);

        Write(path, stream =>
        {
            using var writer = new StreamWriter(stream, encoding, BufferSize, leaveOpen: true);
            writer.Write(content);
            writer.Flush();
        });
    }

    public void WriteBytes(string path, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        Write(path, stream => stream.Write(content));
    }

    private void Write(string path, Action<FileStream> writeContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var destinationPath = System.IO.Path.GetFullPath(path);
        var directory = System.IO.Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new AtomicFileWriteException(
                destinationPath,
                new InvalidOperationException("Filens mappe kunne ikke bestemmes."));
        }

        var temporaryPath = System.IO.Path.Combine(
            directory,
            $".{System.IO.Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
        var committed = false;

        try
        {
            Directory.CreateDirectory(directory);
            _stageCallback?.Invoke(AtomicWriteStage.BeforeTemporaryFileCreation);

            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       BufferSize,
                       FileOptions.WriteThrough))
            {
                writeContent(stream);
                stream.Flush(flushToDisk: true);
            }

            _stageCallback?.Invoke(AtomicWriteStage.AfterTemporaryFileFlush);
            _stageCallback?.Invoke(AtomicWriteStage.BeforeDestinationReplace);
            File.Move(temporaryPath, destinationPath, overwrite: true);
            committed = true;
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or InvalidOperationException)
        {
            throw new AtomicFileWriteException(destinationPath, exception);
        }
        finally
        {
            if (!committed)
            {
                TryDeleteTemporaryFile(temporaryPath);
            }
        }
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
