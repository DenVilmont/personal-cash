using Domain.Enums;

namespace Domain.Contracts.FiltersState
{
    public sealed class TransactionsFilterStateDto
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public DateOnly? Month { get; set; }

        public List<Guid> AccountIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();

        public EntryType? SelectedEntryType { get; set; }
        public bool? IsForPlanning { get; set; }

        public string? Note { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }
}
