# Revision 2.1 — audit fixes

- Login failure counters now update under a database transaction and row lock. Failed attempts commit before an error is returned; lockout uses the SQL Server clock.
- Startup serializes database setup between app instances, detects legacy schemas, applies missing integrity upgrades and refreshes configuration when Retry is pressed.
- Demo accounts can be disabled with `SeedDemoAccounts=false` in the executable configuration.
- Card expiry handles invalid years without throwing. All-zero card numbers, invalid payment amounts, unsupported baggage, oversized passenger lists and invalid seating capacities are rejected.
- Phone, email, names, passenger fields, fares and passport inputs have additional validation. Duplicate traveller passports are rejected.
- Payment rechecks the current flight schedule, fares, capacity and status. Changed flights require a fresh search instead of retaining an obsolete quote.
- Seat selection handles load failures and newly occupied seats; auto-assignment clears stale selections. Nine-traveller lists scroll and narrow seat maps support horizontal scrolling.
- Cancellation compares the displayed refund against the final transaction calculation and requests review if the amount changed. A refund requires an original charge.
- SQL constraints enforce consistent flight/cabin links, valid monetary totals, valid ticket fields, bounded capacity and unique charge/refund references.
- Ticket rendering disposes temporary fonts and failed bitmaps, validates rendering scale and clips long text. Ticket images scale to their preview area.
- Forms use smaller initial sizes, corrected anchors, scrolling and bounded input fields. Source indentation has been normalized.

## Verification

C# grammar parsing and the available source/project checks pass for all 46 C# files. ZIP integrity is checked. Regression tests include additional invalid expiry, zero card and invalid charge cases.

This environment has no C# compiler, Windows desktop or SQL Server. No compilation, C# test execution, database execution or visual desktop testing is claimed. Run `BUILD_AND_TEST.cmd` on Windows and follow `VALIDATION.md` before relying on the app.

The gateway and flight inventory remain demonstrations. Real payments and airline-issued tickets require provider integrations and a durable payment workflow; changing the gateway implementation alone is insufficient for production use.
