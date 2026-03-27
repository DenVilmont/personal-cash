using Domain.Contracts;
using Domain.Contracts.FiltersState;
using Domain.Ports;
using Infrastructure.Mapping;
using Infrastructure.Models;
using Infrastructure.Persistence;
using System.Globalization;
using static Supabase.Postgrest.Constants;

namespace Infrastructure.Repositories
{
    public sealed class TransactionsRepository(DatabaseService db) : ITransactionsRepository
    {
        private readonly DatabaseService _db = db;

        public async Task<IReadOnlyList<TransactionDto>> ListAsync(CancellationToken ct = default)
        {
            var models = await _db.From<Transaction>();
            return models.Select(x => x.ToDto()).ToList();
        }

        public async Task<IReadOnlyList<TransactionDto>> ListAsync(
    TransactionsFilterStateDto filter, CancellationToken ct = default)
        {
            var from = filter.From;
            var to = filter.To;
            if (filter.Month.HasValue)
            {
                from = new DateOnly(filter.Month.Value.Year, filter.Month.Value.Month, 1);
                to = from.Value.AddMonths(1).AddDays(-1);
            }

            var models = await _db.From<Transaction>(q =>
            {
                dynamic dq = q;

                if (from.HasValue)
                    dq = dq.Filter("occurred_on", Operator.GreaterThanOrEqual, from.Value.ToString("yyyy-MM-dd"));
                if (to.HasValue)
                    dq = dq.Filter("occurred_on", Operator.LessThanOrEqual, to.Value.ToString("yyyy-MM-dd"));

                if (filter.SelectedEntryType.HasValue)
                    dq = dq.Filter("entry_type", Operator.Equals, ((int)filter.SelectedEntryType.Value).ToString());

                if (filter.IsForPlanning.HasValue)
                    dq = dq.Filter("is_planned", Operator.Equals, filter.IsForPlanning.Value.ToString().ToLower());

                if (filter.AccountIds.Count > 0)
                    dq = dq.Filter("account_id", Operator.In, filter.AccountIds.Select(x => x.ToString()).ToList());

                if (filter.CategoryIds.Count > 0)
                    dq = dq.Filter("category_id", Operator.In, filter.CategoryIds.Select(x => x.ToString()).ToList());

                if (!string.IsNullOrWhiteSpace(filter.Note))
                    dq = dq.Filter("note", Operator.ILike, $"%{filter.Note.Trim()}%");

                if (filter.MinAmount.HasValue)
                    dq = dq.Filter("amount", Operator.GreaterThanOrEqual,
                        filter.MinAmount.Value.ToString(CultureInfo.InvariantCulture));
                if (filter.MaxAmount.HasValue)
                    dq = dq.Filter("amount", Operator.LessThanOrEqual,
                        filter.MaxAmount.Value.ToString(CultureInfo.InvariantCulture));

                // Order требует 3 аргумента через dynamic — необязательный параметр не передаётся автоматически
                dq = dq.Order("occurred_on", Ordering.Descending, NullPosition.Last);
                dq = dq.Order("created_at", Ordering.Descending, NullPosition.Last);

                return dq;
            });

            return models.Select(x => x.ToDto()).ToList();
        }

        public async Task<TransactionDto> InsertReturningAsync(TransactionDto tx, CancellationToken ct = default)
        {
            var inserted = (await _db.Insert(tx.ToModel())).Single();
            return inserted.ToDto();
        }

        public async Task UpdateAsync(TransactionDto tx, CancellationToken ct = default)
        {
            await _db.Update(tx.ToModel());
        }

        public async Task DeleteAsync(TransactionDto tx, CancellationToken ct = default)
        {
            await _db.Delete(tx.ToModel());
        }
    }
}
