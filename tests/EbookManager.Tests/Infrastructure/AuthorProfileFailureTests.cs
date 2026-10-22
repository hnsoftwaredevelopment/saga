using EbookManager.Application.Authors;
using EbookManager.Domain.Abstractions;
using EbookManager.Domain.Authors;
using FluentAssertions;

namespace EbookManager.Tests.Infrastructure;

public sealed class AuthorProfileFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_failed_save_preserves_old_photo_but_a_confirmed_commit_keeps_new_photo(bool commitBeforeFailure)
    {
        var repository = new FailingRepository { CommitBeforeFailure = commitBeforeFailure };
        var photos = new RecordingPhotos();
        var service = new AuthorProfileService(repository, photos);
        var original = repository.Profile;
        if (!commitBeforeFailure)
        {
            var act = () => service.SaveAsync(original, "changed", null, [1], false, default);
            await act.Should().ThrowAsync<IOException>();
            repository.Profile.Should().Be(original);
            photos.Deleted.Should().Equal("new-photo");
        }
        else
        {
            var result = await service.SaveAsync(original, "changed", null, [1], false, default);
            result.Status.Should().Be(AuthorProfileSaveStatus.Saved);
            repository.Profile.PhotoRelativePath.Should().Be("new-photo");
            photos.Deleted.Should().Equal("old-photo");
        }
    }

    [Fact]
    public async Task Uncertain_commit_with_failed_reload_never_deletes_either_photo()
    {
        var repository = new FailingRepository { FailReload = true };
        var photos = new RecordingPhotos();
        var service = new AuthorProfileService(repository, photos);
        var act = () => service.SaveAsync(repository.Profile, "changed", null, [1], false, default);
        await act.Should().ThrowAsync<IOException>();
        photos.Deleted.Should().BeEmpty();
    }

    private sealed class FailingRepository : IAuthorProfileRepository
    {
        public AuthorProfile Profile = new(Guid.NewGuid(), "Author", "original", null, "old-photo");
        public bool CommitBeforeFailure;
        public bool FailReload;
        public Task<AuthorProfile?> GetAsync(Guid id, CancellationToken token) => FailReload
            ? Task.FromException<AuthorProfile?>(new IOException("Read failed")) : Task.FromResult<AuthorProfile?>(Profile);
        public Task<AuthorProfileSaveStatus> SaveAsync(AuthorProfile original, AuthorProfile updated, CancellationToken token)
        {
            if (CommitBeforeFailure) Profile = updated;
            return Task.FromException<AuthorProfileSaveStatus>(new IOException("Write failed"));
        }
        public Task<AuthorPage> SearchAsync(string query, int skip, int take, CancellationToken token) => throw new NotSupportedException();
        public Task<IReadOnlyList<AuthorBook>> ListBooksAsync(Guid id, int skip, int take, CancellationToken token) => throw new NotSupportedException();
    }

    private sealed class RecordingPhotos : IAuthorPhotoStore
    {
        public List<string> Deleted = [];
        public Task<string> SaveJpegAsync(Guid id, byte[] jpeg, CancellationToken token) => Task.FromResult("new-photo");
        public Task<byte[]?> ReadAsync(Guid id, string path, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, string path, CancellationToken token) { Deleted.Add(path); return Task.CompletedTask; }
    }
}
