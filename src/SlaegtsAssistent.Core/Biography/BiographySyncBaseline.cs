namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographySyncBaseline(
    int Version,
    CanonicalBiographySnapshot Imported,
    CanonicalBiographySnapshot Approved)
{
    public const int CurrentVersion = 1;

    public static BiographySyncBaseline CreateInitial(CanonicalBiographySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new BiographySyncBaseline(CurrentVersion, snapshot, snapshot);
    }
}

public enum BiographyBaselineStatus
{
    Unchanged,
    Changed,
    Missing,
    UnsupportedVersion,
}

public sealed record BiographyReconciliationState(
    BiographyBaselineStatus Status,
    CanonicalBiographySnapshot Imported,
    CanonicalBiographySnapshot? Approved,
    BiographyFactsSnapshot DocumentFacts)
{
    public bool RequiresReview => Status != BiographyBaselineStatus.Unchanged;

    public static BiographyReconciliationState Create(
        BiographySyncBaseline? baseline,
        CanonicalBiographySnapshot imported,
        BiographyFactsSnapshot documentFacts)
    {
        ArgumentNullException.ThrowIfNull(imported);
        ArgumentNullException.ThrowIfNull(documentFacts);

        if (baseline is null)
        {
            return new BiographyReconciliationState(
                BiographyBaselineStatus.Missing,
                imported,
                null,
                documentFacts);
        }

        if (baseline.Version != BiographySyncBaseline.CurrentVersion
            || baseline.Imported.Version != CanonicalBiographySnapshot.CurrentVersion
            || baseline.Approved.Version != CanonicalBiographySnapshot.CurrentVersion)
        {
            return new BiographyReconciliationState(
                BiographyBaselineStatus.UnsupportedVersion,
                imported,
                baseline.Approved,
                documentFacts);
        }

        var status = string.Equals(
            baseline.Approved.ComputeFingerprint(),
            imported.ComputeFingerprint(),
            StringComparison.Ordinal)
            ? BiographyBaselineStatus.Unchanged
            : BiographyBaselineStatus.Changed;
        return new BiographyReconciliationState(status, imported, baseline.Approved, documentFacts);
    }
}
