alter table public.accounts
add column if not exists account_type text not null default 'Regular';

alter table public.accounts
add column if not exists parent_account_id uuid null;

alter table public.accounts
drop constraint if exists accounts_account_type_check;

alter table public.accounts
add constraint accounts_account_type_check
check (account_type in ('Regular', 'Group'));

alter table public.accounts
drop constraint if exists accounts_group_parent_null_check;

alter table public.accounts
add constraint accounts_group_parent_null_check
check (
  account_type <> 'Group'
  or parent_account_id is null
);

alter table public.accounts
drop constraint if exists accounts_parent_account_fk;

alter table public.accounts
add constraint accounts_parent_account_fk
foreign key (user_id, parent_account_id)
references public.accounts (user_id, id)
on delete restrict;

create index if not exists accounts_parent_account_id_idx
on public.accounts(parent_account_id);

update public.accounts
set account_type = 'Regular'
where account_type is null;