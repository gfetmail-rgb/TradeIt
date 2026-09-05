# TradeIt Architecture

## Boundaries

TradeIt is organized around four responsibilities:

- **Models** — domain/state objects (`Portfolio`, `SymbolInfo`, `MarketBar`, settings models).
- **Data** — parsing and validation of source files. Invalid market data is not silently repaired.
- **Services** — application/business logic and persistence orchestration.
- **Views / WPF code-behind** — UI event wiring, presentation state, and delegation to services.

## Symbol data flow

`MainWindow` → `SymbolDataService` → `SymbolUniverseService` / `MarketBarDataService`

Filtering is delegated to `SymbolFilterEngine`, including historical-bar caching and numeric filter evaluation.

## Chart data flow

`ChartTabView` remains the WPF facade for a chart, while chart behavior is separated into concern-based partials:

- display and chart rendering
- interaction and axes
- time-axis handling
- settings synchronization
- initial range
- crosshair
- drawing tools, selection, handles, text and technical drawings

The partials are intentionally organized by responsibility; they are not duplicate implementations of the same feature.

## Auto Scroll

`AutoScrollController` owns timer state, index progression, concurrency protection, and UI-context marshaling. `MainWindow.AutoScroll.cs` is the UI adapter that creates/reuses the Auto Scroll tab and delegates progression to the controller.

## Persistence

- `PortfolioManager` owns portfolio persistence.
- `ChartSettingsManager` owns chart-settings persistence.
- `SymbolIdentityStore` owns symbol identity persistence.
- `StoragePaths` centralizes storage locations and performs one-time legacy migration.

## Refactoring rules

1. Keep business logic out of WPF event handlers when it can live in a service/engine.
2. Do not introduce a second implementation of an existing behavior to fix a UI issue.
3. Prefer a single authoritative state owner for timers, filters, settings, and persistence.
4. Preserve user-visible behavior unless a change is explicitly required.
5. Never hide malformed market data by silently substituting values.
6. Keep compatibility helpers next to the subsystem they serve; avoid standalone compatibility files once consolidation is complete.
