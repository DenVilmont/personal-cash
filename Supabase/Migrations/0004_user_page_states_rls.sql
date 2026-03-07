alter table public.user_page_states enable row level security;

create policy "user_page_states_select_own"
on public.user_page_states
for select
to authenticated
using (user_id = auth.uid());

create policy "user_page_states_insert_own"
on public.user_page_states
for insert
to authenticated
with check (user_id = auth.uid());

create policy "user_page_states_update_own"
on public.user_page_states
for update
to authenticated
using (user_id = auth.uid())
with check (user_id = auth.uid());

create policy "user_page_states_delete_own"
on public.user_page_states
for delete
to authenticated
using (user_id = auth.uid());

revoke all on table public.user_page_states from anon, authenticated;
grant select, insert, update, delete on table public.user_page_states to authenticated;