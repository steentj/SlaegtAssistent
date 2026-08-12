namespace SlaegtsAssistent.App.Services;

public enum AtomicWriteStage
{
    BeforeTemporaryFileCreation,
    AfterTemporaryFileFlush,
    BeforeDestinationReplace,
}
