using EbookManager.Presentation.ViewModels;
using FluentAssertions;

namespace EbookManager.Tests.App.ViewModels;

public sealed class BookshelfSelectionStateTests
{
    [Fact]
    public void Click_toggle_range_and_additive_range_follow_unique_display_order()
    {
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var state = new BookshelfSelectionState();
        var chosen = state.Select(ids, [], ids[1], false, false);
        chosen.Should().Equal(ids[1]);
        chosen = state.Select(ids, chosen, ids[4], true, false);
        chosen.Should().Equal(ids[1], ids[4]);
        chosen = state.Select(ids, chosen, ids[4], true, false);
        chosen.Should().Equal(ids[1]);
        chosen = state.Select(ids, chosen, ids[2], false, true);
        chosen.Should().Equal(ids[2], ids[3], ids[4]);
        chosen = state.Select(ids, chosen, ids[5], true, true);
        chosen.Should().Equal(ids[2], ids[3], ids[4], ids[5]);
        state.Select(ids, chosen, ids[0], false, false).Should().Equal(ids[0]);
    }

    [Fact]
    public void Missing_or_reset_anchor_selects_target_without_hidden_books_and_deduplicates_groups()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var state = new BookshelfSelectionState();
        state.Select([a, b, c], [], a, false, false);
        state.Select([b, b, c], [a], c, false, true).Should().Equal(c);
        state.Reset();
        state.Select([a, b, c], [a], b, false, true).Should().Equal(b);
        state.Select([a, b, b, c], [], c, false, true).Should().Equal(b, c);
    }
}
