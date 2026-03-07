create table public.user_page_states (
  id uuid not null default gen_random_uuid (),
  user_id uuid not null default auth.uid (),
  page_key text not null,
  state_json text not null default '{}'::text,
  created_at timestamp with time zone not null default now(),
  updated_at timestamp with time zone not null default now(),

  constraint user_page_states_pkey primary key (id),
  constraint user_page_states_user_page_key_unique unique (user_id, page_key),
  constraint user_page_states_user_id_fkey foreign key (user_id) references auth.users (id) on delete cascade,
  constraint user_page_states_page_key_nonempty check (length(btrim(page_key)) > 0)
) tablespace pg_default;

create index if not exists user_page_states_user_page_idx
  on public.user_page_states using btree (user_id, page_key) tablespace pg_default;

create trigger trg_user_page_states_updated_at
before update on public.user_page_states
for each row
execute function public.set_updated_at();