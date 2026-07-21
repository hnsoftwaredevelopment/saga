namespace EbookManager.Domain.Settings;

public enum DuplicateMergeDefaultAction
{
    NoAction,
    Copy,
    Merge
}

public sealed record DuplicateMergeDefaultSettings(
    DuplicateMergeDefaultAction Cover = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Title = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Authors = DuplicateMergeDefaultAction.Merge,
    DuplicateMergeDefaultAction Series = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction SeriesNumber = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Language = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Publisher = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction PublicationDate = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Isbn = DuplicateMergeDefaultAction.NoAction,
    DuplicateMergeDefaultAction Tags = DuplicateMergeDefaultAction.Merge,
    DuplicateMergeDefaultAction Description = DuplicateMergeDefaultAction.NoAction);
