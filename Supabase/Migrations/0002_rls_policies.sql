alter table public.accounts enable row level security;
alter table public.transactions enable row level security;
alter table public.categories enable row level security;
alter table public.loans enable row level security;
alter table public.loan_payments enable row level security;
alter table public.user_settings enable row level security;


create policy "accounts_delete_own" on public.accounts for delete to authenticated using ((user_id = auth.uid()));
create policy "accounts_insert_own" on public.accounts for insert to authenticated with check ((user_id = auth.uid())); 
create policy "accounts_select_own" on public.accounts for select to authenticated using ((user_id = auth.uid()));
create policy "accounts_update_own" on public.accounts for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));
create policy "categories_delete_own" on public.categories for delete to authenticated using ((user_id = auth.uid()));
create policy "categories_insert_own" on public.categories for insert to authenticated with check ((user_id = auth.uid())); 
create policy "categories_select_own" on public.categories for select to authenticated using ((user_id = auth.uid()));
create policy "categories_update_own" on public.categories for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));
create policy "loan_payments_delete" on public.loan_payments for delete to authenticated using ((user_id = auth.uid()));
create policy "loan_payments_insert" on public.loan_payments for insert to authenticated with check ((user_id = auth.uid())); 
create policy "loan_payments_select" on public.loan_payments for select to authenticated using ((user_id = auth.uid()));
create policy "loan_payments_update" on public.loan_payments for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));
create policy "loans_delete" on public.loans for delete to authenticated using ((user_id = auth.uid()));
create policy "loans_insert" on public.loans for insert to authenticated with check ((user_id = auth.uid())); 
create policy "loans_select" on public.loans for select to authenticated using ((user_id = auth.uid()));
create policy "loans_update" on public.loans for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));
create policy "transactions_delete_own" on public.transactions for delete to authenticated using ((user_id = auth.uid()));
create policy "transactions_insert_own" on public.transactions for insert to authenticated with check ((user_id = auth.uid())); 
create policy "transactions_select_own" on public.transactions for select to authenticated using ((user_id = auth.uid()));
create policy "transactions_update_own" on public.transactions for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));
create policy "user_settings_delete_own" on public.user_settings for delete to authenticated using ((user_id = auth.uid()));
create policy "user_settings_insert_own" on public.user_settings for insert to authenticated with check ((user_id = auth.uid())); 
create policy "user_settings_select_own" on public.user_settings for select to authenticated using ((user_id = auth.uid()));
create policy "user_settings_update_own" on public.user_settings for update to authenticated using ((user_id = auth.uid())) with check ((user_id = auth.uid()));


grant usage on schema public to authenticated;
revoke usage on schema public from anon;
revoke all on table public.accounts from anon, authenticated;
grant select, insert, update, delete on table public.accounts to authenticated;
revoke all on table public.categories from anon, authenticated;
grant select, insert, update, delete on table public.categories to authenticated;
revoke all on table public.transactions from anon, authenticated;
grant select, insert, update, delete on table public.transactions to authenticated;
revoke all on table public.loans from anon, authenticated;
grant select, insert, update, delete on table public.loans to authenticated;
revoke all on table public.loan_payments from anon, authenticated;
grant select, insert, update, delete on table public.loan_payments to authenticated;
revoke all on table public.user_settings from anon, authenticated;
grant select, insert, update, delete on table public.user_settings to authenticated;