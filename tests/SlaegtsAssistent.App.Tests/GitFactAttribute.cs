using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class GitFactAttribute : FactAttribute
{
    public GitFactAttribute()
    {
        if (new GitLocator().FindGitExecutable() is null)
        {
            Skip = "Git blev ikke fundet i testmiljøet.";
        }
    }
}
