# Smoke Bench — Throughput & Stability Results

This file records observed results from `InputThroughputSmokeTests.cs`. These are NOT
pass/fail CI gates — every test in that file asserts correctness only (every handler/
subscriber received the right number of calls, in the right order). Timing is printed
to stdout and copied here manually as a reference snapshot whenever something notable
changes (e.g. a dispatch path optimization, a regression worth tracking, or a new
comparison worth keeping).

**These tests are isolated from the default test run** via `[Trait("Category", "Smoke")]`
on the whole class, plus a `.runsettings` default exclusion (`Category!=Smoke`). A plain
`dotnet test` will NOT run them. See "How to run" below.

---

## Run 1 — Subscriber count × event count sweep

`StabilityLog_SubscriberCount_x_EventCount_Sweep`

Distinct `FakeSubscriber` instances (simulating separate classes like Player, UIManager,
AudioController each subscribing independently) bound via `BindAction`, swept against
event burst size.

```
[Stability] subscribers | events | presses | total_ms   | ms/event  | ms/invocation
[Stability] -------------------------------------------------------------------------
[Stability]           1 |     50 |      25 |    0,1231 |   0,00246 |      0,004924
[Stability]           1 |    100 |      50 |    0,0413 |   0,00041 |      0,000826
[Stability]           1 |    350 |     175 |    0,1463 |   0,00042 |      0,000836
[Stability]           1 |   1000 |     500 |    0,4451 |   0,00045 |      0,000890
[Stability]           2 |     50 |      25 |    0,0236 |   0,00047 |      0,000472
[Stability]           2 |    100 |      50 |    0,0413 |   0,00041 |      0,000413
[Stability]           2 |    350 |     175 |    0,1495 |   0,00043 |      0,000427
[Stability]           2 |   1000 |     500 |    0,4228 |   0,00042 |      0,000423
[Stability]           5 |     50 |      25 |    0,0215 |   0,00043 |      0,000172
[Stability]           5 |    100 |      50 |    0,0447 |   0,00045 |      0,000179
[Stability]           5 |    350 |     175 |    0,1518 |   0,00043 |      0,000173
[Stability]           5 |   1000 |     500 |    0,4380 |   0,00044 |      0,000175
[Stability]          10 |     50 |      25 |    0,0257 |   0,00051 |      0,000103
[Stability]          10 |    100 |      50 |    0,0518 |   0,00052 |      0,000104
[Stability]          10 |    350 |     175 |    0,1598 |   0,00046 |      0,000091
[Stability]          10 |   1000 |     500 |    0,4669 |   0,00047 |      0,000093
[Stability]          25 |     50 |      25 |    0,0293 |   0,00059 |      0,000047
[Stability]          25 |    100 |      50 |    0,0605 |   0,00060 |      0,000048
[Stability]          25 |    350 |     175 |    0,1906 |   0,00054 |      0,000044
[Stability]          25 |   1000 |     500 |    0,5457 |   0,00055 |      0,000044
[Stability]          50 |     50 |      25 |    0,0334 |   0,00067 |      0,000027
[Stability]          50 |    100 |      50 |    0,0698 |   0,00070 |      0,000028
[Stability]          50 |    350 |     175 |    0,2371 |   0,00068 |      0,000027
[Stability]          50 |   1000 |     500 |    0,8101 |   0,00081 |      0,000032
[Stability]         100 |     50 |      25 |    0,0514 |   0,00103 |      0,000021
[Stability]         100 |    100 |      50 |    0,0884 |   0,00088 |      0,000018
[Stability]         100 |    350 |     175 |    0,3134 |   0,00090 |      0,000018
[Stability]         100 |   1000 |     500 |    0,9005 |   0,00090 |      0,000018
[Stability]         250 |     50 |      25 |    0,0772 |   0,00154 |      0,000012
[Stability]         250 |    100 |      50 |    0,1542 |   0,00154 |      0,000012
[Stability]         250 |    350 |     175 |    0,5566 |   0,00159 |      0,000013
[Stability]         250 |   1000 |     500 |    1,5856 |   0,00159 |      0,000013
[Stability]         500 |     50 |      25 |    0,1358 |   0,00272 |      0,000011
[Stability]         500 |    100 |      50 |    0,2638 |   0,00264 |      0,000011
[Stability]         500 |    350 |     175 |    0,9379 |   0,00268 |      0,000011
[Stability]         500 |   1000 |     500 |    3,0767 |   0,00308 |      0,000012
[Stability] -------------------------------------------------------------------------
```

**Reading:** ms/event grows gently from ~0.0004ms at 1 subscriber to ~0.003ms at 500
subscribers — roughly a 6–7x cost increase for a 500x subscriber increase. That's far
better than linear (O(n)), let alone O(n²). Even the worst case (500 subscribers × 1000
events = 500,000 callback invocations) completes in ~3ms. No evidence of pathological
scaling in the dispatch path.

---

## Run 2 — InputForge vs. raw `_Input()` override, by handler count

`StabilityLog_InputForge_vs_RawInputOverride_ByHandlerCount`

Same simple logic expression (`if (event is InputEventKey { Keycode: Key.Space, Pressed:
true }) CallCount++;`) implemented two ways, at a fixed 300-event burst:

1. **InputForge** — N distinct subscriber instances bound via `BindAction` to a single
   `EnhancedInputSystem` + `InputMappingContext` pipeline (one `_Input()` override total).
2. **Raw Godot** — N distinct `Node` instances, each overriding `_Input()` itself and
   running the identical check independently (N `_Input()` overrides, one per node).

```
[Stability] handlers |  InputForge ms_total  ms/handler  |  RawInput ms_total  ms/handler  |  ratio
[Stability] -------------------------------------------------------------------------------------
[Stability]       10 |             0,1926    0,019260  |          0,2857    0,028570  |   1,48x
[Stability]       25 |             0,1687    0,006748  |          0,2726    0,010904  |   1,62x
[Stability]       50 |             0,1957    0,003914  |          0,5362    0,010724  |   2,74x
[Stability]      100 |             0,2632    0,002632  |          1,0652    0,010652  |   4,05x
[Stability]      250 |             0,4599    0,001840  |          2,6360    0,010544  |   5,73x
[Stability]      500 |             0,8029    0,001606  |          5,4249    0,010850  |   6,76x
[Stability] -------------------------------------------------------------------------------------
```

**Reading:** the `ratio` column (Raw ms/handler ÷ InputForge ms/handler) climbs steadily
as handler count grows — InputForge is ~1.5x cheaper at 10 handlers and **~6.8x cheaper
at 500 handlers**. This makes sense: InputForge pays Godot's native `_Input()` dispatch
cost exactly once per event regardless of subscriber count, then fans out via a single
in-process `foreach` over a delegate list. The raw approach pays Godot's `_Input()`
dispatch cost once *per node, per event* — so its overhead scales with handler count
while InputForge's stays close to flat.

**Caveat:** the raw side here calls `node._Input(evt)` directly per node in this test —
it does **not** pay for Godot's own native `SceneTree` dispatch/marshalling overhead per
node (interop, virtual call resolution, etc. for each of N real nodes). In a real running
game with N actual `_Input()`-overriding nodes in the tree, the raw column's real-world
cost — and therefore the ratio in InputForge's favor — would likely be even higher than
shown here. This benchmark is a lower bound on how much more expensive N real `_Input()`
overrides would be in practice.

---

## How to run

A plain `dotnet test` SKIPS these (default-excluded via `.runsettings`). To run them
explicitly:

```
dotnet test --filter "Category=Smoke" -v detailed
```

Or run the `InputThroughputSmokeTests` class from Rider's test runner (Rider does not
honor `.runsettings` test-case filters by default for an "all tests" run, so the class
will still appear — just be aware it's excluded from `dotnet test` / CI by default) and
check the **Output** panel for each test (not just pass/fail) — the `[Smoke]` /
`[Stability]` lines are printed there.
