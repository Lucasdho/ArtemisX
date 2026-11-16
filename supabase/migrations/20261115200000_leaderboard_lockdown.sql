-- Lock the leaderboard down for the anonymous game client:
--   anon/authenticated may INSERT runs (column grants + checks + insert guard from earlier migrations)
--   and SELECT only the aggregated run_histogram view. Individual rows (nicknames) are never readable.

-- 1. No direct reads of runs or the per-player top_runs view.
drop policy if exists "anon can read runs" on public.runs;
revoke all on table public.runs from anon, authenticated;
grant insert (nickname, character_index, days_taken, stars, end_reason) on public.runs to anon, authenticated;
revoke all on table public.top_runs from anon, authenticated;

-- 2. Aggregation runs as the owner inside a schema PostgREST does not expose.
create schema if not exists leaderboard_internal;
revoke all on schema leaderboard_internal from public;
grant usage on schema leaderboard_internal to anon, authenticated;

create or replace function leaderboard_internal.run_histogram_rows()
returns table (days_taken smallint, end_reason text, runs integer)
language sql
stable
security definer
set search_path = ''
as $$
  select r.days_taken, r.end_reason, count(*)::integer
  from public.runs r
  group by r.days_taken, r.end_reason
$$;
revoke all on function leaderboard_internal.run_histogram_rows() from public;
grant execute on function leaderboard_internal.run_histogram_rows() to anon, authenticated;

-- 3. Same REST path and columns the client already reads (GET /rest/v1/run_histogram).
create or replace view public.run_histogram with (security_invoker = true) as
  select h.days_taken, h.end_reason, h.runs
  from leaderboard_internal.run_histogram_rows() h;
revoke all on table public.run_histogram from anon, authenticated;
grant select on table public.run_histogram to anon, authenticated;
