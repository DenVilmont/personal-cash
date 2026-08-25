alter table public.transactions
add column if not exists destination_account_id uuid null;

alter table public.categories
add column if not exists is_transfer_category boolean not null default false;

alter table public.transactions
drop constraint if exists transactions_entry_type_check;

alter table public.transactions
add constraint transactions_entry_type_check
check (entry_type = any (array[0, 1, 2]));