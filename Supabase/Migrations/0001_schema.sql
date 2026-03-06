create extension if not exists pgcrypto;

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

create table public.categories (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null default auth.uid (),
  name text not null,
  created_at timestamp with time zone not null default now(),
  constraint categories_pkey primary key (id),
  constraint categories_user_id_id_unique unique (user_id, id),
  constraint categories_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE,
  constraint categories_name_nonempty check ((length(btrim(name)) > 0))
) TABLESPACE pg_default;

create index IF not exists categories_user_id_idx on public.categories using btree (user_id) TABLESPACE pg_default;

create unique INDEX IF not exists categories_user_name_ci_unique_idx on public.categories using btree (user_id, lower(btrim(name))) TABLESPACE pg_default;



create table public.accounts (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null default auth.uid (),
  name text not null,
  currency text not null default 'EUR'::text,
  icon_key text not null default 'Wallet'::text,
  balance_actual numeric(14, 2) not null default 0,
  balance_expected numeric(14, 2) not null default 0,
  is_archived boolean not null default false,
  created_at timestamp with time zone not null default now(),
  updated_at timestamp with time zone not null default now(),
  show_balance boolean not null default true,
  constraint accounts_pkey primary key (id),
  constraint accounts_user_id_id_key unique (user_id, id),
  constraint accounts_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE,
  constraint accounts_name_nonempty check ((length(btrim(name)) > 0))
) TABLESPACE pg_default;

create index IF not exists accounts_user_active_idx on public.accounts using btree (user_id, is_archived) TABLESPACE pg_default;

create index IF not exists accounts_user_id_idx on public.accounts using btree (user_id) TABLESPACE pg_default;

create unique INDEX IF not exists accounts_user_name_ci_unique_idx on public.accounts using btree (user_id, lower(btrim(name))) TABLESPACE pg_default;

create trigger accounts_set_updated_at BEFORE
update on public.accounts for EACH row
execute FUNCTION public.set_updated_at ();


create table public.transactions (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null default auth.uid (),
  occurred_on date not null,
  amount numeric(12, 2) not null,
  entry_type smallint not null,
  is_planned boolean not null default false,
  currency character(3) not null default 'EUR'::bpchar,
  note text null,
  created_at timestamp with time zone not null default now(),
  category_id uuid null,
  account_id uuid not null,
  constraint transactions_pkey primary key (id),
  constraint transactions_user_category_fkey foreign KEY (user_id, category_id) references public.categories (user_id, id) on delete RESTRICT,
  constraint transactions_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE,
  constraint transactions_account_fk foreign KEY (user_id, account_id) references public.accounts (user_id, id) on delete RESTRICT,
  constraint transactions_entry_type_check check ((entry_type = any (array[0, 1]))),
  constraint transactions_amount_check check ((amount >= (0)::numeric)),
  constraint transactions_currency_check check ((currency ~ '^[A-Z]{3}$'::text))
) TABLESPACE pg_default;

create index IF not exists transactions_user_account_date_idx on public.transactions using btree (user_id, account_id, occurred_on desc) TABLESPACE pg_default;

create index IF not exists transactions_user_date_idx on public.transactions using btree (user_id, occurred_on desc) TABLESPACE pg_default;

create index IF not exists transactions_user_type_date_idx on public.transactions using btree (user_id, entry_type, occurred_on desc) TABLESPACE pg_default;

create index IF not exists transactions_category_id_idx on public.transactions using btree (category_id) TABLESPACE pg_default;



create table public.loans (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null,
  name text not null,
  currency text not null default 'EUR'::text,
  amount numeric not null,
  payments_count integer not null,
  start_date date not null,
  has_interest boolean not null default false,
  interest_rate numeric null,
  note text null,
  created_at timestamp with time zone not null default now(),
  updated_at timestamp with time zone not null default now(),
  constraint loans_pkey primary key (id),
  constraint loans_user_id_id_unique unique (user_id, id),
  constraint loans_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE,
  constraint loans_amount_check check ((amount >= (0)::numeric)),
  constraint loans_payments_count_check check ((payments_count > 0))
) TABLESPACE pg_default;

create trigger loans_set_updated_at
before update on public.loans
for each row
execute function public.set_updated_at();




create table public.loan_payments (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null,
  loan_id uuid not null,
  due_date date not null,
  amount numeric not null,
  is_paid boolean not null default false,
  note text null,
  created_at timestamp with time zone not null default now(),
  updated_at timestamp with time zone not null default now(),
  constraint loan_payments_pkey primary key (id),
  constraint loan_payments_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE,
  constraint loan_payments_user_loan_fkey foreign KEY (user_id, loan_id) references public.loans (user_id, id) on delete CASCADE,
  constraint loan_payments_amount_check check ((amount >= (0)::numeric))
) TABLESPACE pg_default;

create index IF not exists ix_loan_payments_loan_due on public.loan_payments using btree (loan_id, due_date) TABLESPACE pg_default;

create trigger loan_payments_set_updated_at
before update on public.loan_payments
for each row
execute function public.set_updated_at();



create table public.user_settings (
  user_id uuid not null,
  first_name text null,
  last_name text null,
  preferred_language text not null default 'en'::text,
  preferred_currency text not null default 'EUR'::text,
  created_at timestamp with time zone not null default now(),
  updated_at timestamp with time zone not null default now(),
  avatar_base64 text null,
  avatar_mime text null,
  constraint user_settings_pkey primary key (user_id),
  constraint user_settings_user_id_fkey foreign KEY (user_id) references auth.users (id) on delete CASCADE
) TABLESPACE pg_default;

create trigger trg_user_settings_updated_at BEFORE
update on public.user_settings for EACH row
execute FUNCTION public.set_updated_at ();


