using EbookManager.Application.Authors;
using EbookManager.Domain.Authors;

namespace EbookManager.Presentation.ViewModels;

public sealed partial class AuthorManagementViewModel
{
    private AuthorMergePreview? candidatePreview;
    public bool CanFindDuplicates => CanChooseAuthors && mergeService is not null;
    public IAuthorDuplicateIndex CreateDuplicateIndex() => new AuthorDuplicateSearchService(repository);

    public async Task<AuthorMergePreview?> PreviewCandidateAsync(AuthorDuplicateCandidate candidate)
    {
        if (!CanFindDuplicates || HasChanges || candidate.First.Id == candidate.Second.Id) return null;
        candidatePreview = null;
        IsBusy = true;
        try
        {
            Guid[] ids = [candidate.First.Id, candidate.Second.Id];
            var preview = await Task.Run(() => mergeService!.PreviewAsync(ids, lifetime.Token), lifetime.Token);
            if (disposed) return null;
            if (preview is null || !ids.ToHashSet().SetEquals(preview.Authors.Select(author => author.Profile.Id)) ||
                preview.Authors.Any(author => author.Profile.Name != (author.Profile.Id == candidate.First.Id ? candidate.First.Name : candidate.Second.Name)))
            { SetStatus("AuthorMergeConflict", true); return null; }
            return candidatePreview = preview;
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception) { if (!disposed) SetStatus("AuthorProfileLoadFailed", true); return null; }
        finally { if (!disposed) IsBusy = false; }
    }
    public Task<bool> MergeCandidateAsync(AuthorMergePreview preview, AuthorMergeSelection selection)
    {
        if (!CanFindDuplicates || HasChanges || !ReferenceEquals(preview, candidatePreview)) return Task.FromResult(false);
        candidatePreview = null;
        return MergeCoreAsync(preview, selection);
    }
}
