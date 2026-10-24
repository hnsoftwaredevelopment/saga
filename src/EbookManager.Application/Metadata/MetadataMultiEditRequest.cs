using EbookManager.Domain.Books;
using EbookManager.Domain.CustomMetadata;

namespace EbookManager.Application.Metadata;

public enum MultiEditTagAction { Replace, Add, Remove }

public sealed record MetadataMultiEditRequest(
    bool UpdateAuthors, string AuthorsText, bool UpdateSeries, string SeriesText,
    bool UpdateTags, string TagsText, bool UpdateLanguage, string LanguageText,
    bool UpdateStatus, ReadingStatus Status, bool SwapTitleAndAuthors = false,
    MultiEditTagAction TagAction = MultiEditTagAction.Replace,
    IReadOnlyList<MultiEditCustomChange>? CustomFields = null);

public sealed record MultiEditCustomChange(Guid FieldId, string Name, CustomMetadataValue? Value);
public enum MultiEditStatus { Updated, Unchanged, Failed, Partial, FileWarning, NotFound, NotProcessed }
public enum MultiEditFailure { None, Read, Duplicate, Save, Custom, CustomUnavailable, Cancelled, Uncertain }
public sealed record MultiEditItem(Guid BookId, MultiEditStatus Status, Book? Book = null,
    MultiEditFailure Failure = MultiEditFailure.None, IReadOnlyList<string>? CustomFields = null, bool HasFileWarning = false);
