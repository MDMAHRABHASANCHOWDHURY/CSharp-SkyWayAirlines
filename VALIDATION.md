# Validation status

## Completed in this environment

- Parsed all 46 C# files with a C# grammar: no syntax errors or missing syntax nodes.
- Checked explicit calls against declared application types: no missing named application members. This limited check does not resolve types, overloads or inherited members.
- Parsed both Visual Studio project XML files and checked every source/resource/content path exists.
- Confirmed the application project includes every application C# source exactly once.
- Confirmed required database tables, availability view and filtered unique active-seat index and additional integrity constraints appear in the SQL script.
- Removed original-machine paths from the revised solution and regression tests.
- Checked the packaged ZIP for archive integrity.

## Not completed here

The environment does not have a working C# compiler, Windows desktop, SQL Server or printer. Compilation, C# regression test execution, SQL execution, concurrent database tests, UI rendering and print output were not performed. The earlier transcript's 96 passing checks describe another environment and are not evidence of this package's runtime correctness. The included regression suite extends those checks, but still needs execution.

## Windows acceptance checklist

1. Build the solution in Debug and Release. Set `AirlineReservationDemo` as startup and launch it.
2. Configure your SQL instance. On a fresh database, verify first-run setup succeeds. Restart and verify no duplicate flights or accounts are created.
3. Set `SkyWay.LogicTests` as startup and run. It should exit with code 0 and report no failed checks.
4. Sign in with both demo accounts. Register a new passenger, test an invalid password and verify lockout after five failures.
5. Search DAC → DXB seven days ahead, then search a domestic route. Test economy/business and 1–9 travellers.
6. Complete bookings for adult/child passengers, check required international passports and select separate seats. Verify fares, baggage and tax totals.
7. Use the approved test card with a future expiry. Verify a PNR and e-ticket for each traveller. The declined test card must save no booking, payment or seat.
8. In two running app instances, try paying for the same seat. Exactly one booking should succeed and the other should report a conflict. Simultaneous edits/cancellation should not leave inconsistent seat state.
9. Cancel bookings in each refund window. Verify ticket status, booking status, refund amount and payment record; verify cancelled seats become available.
10. Cancel an entire flight with several bookings. Verify every booking is cancelled and refunded in full, and the flight disappears from search. Retry after cancellation; no second refund should be recorded.
11. Use admin edit/add/delete screens, including foreign-key restrictions. Attempt a fare change while another window has an old quote; it should require the passenger to search again.
12. Preview several e-tickets, print repeatedly, save PNG and choose Microsoft Print to PDF. Check every traveller appears once, including on repeated print operations.
13. Export bookings CSV. Verify quoting, Unicode routes and formula-like fields in a spreadsheet.
14. Inspect screens at 100%, 125% and 150% Windows scaling; check long names, empty grids, small/maximised windows and all booking steps.

The payment gateway and sample schedule are demonstration components. Real airline inventory, airline-issued ticket numbers, airport time-zone handling, actual payments and email delivery require additional integrations.

15. Try invalid expiry years, an all-zero card, invalid baggage and duplicate passports. Test nine travellers and scroll the seat list. Refresh a seat after another instance buys it.
16. Retry after changing executable configuration. Launch two instances during first-run setup. Verify login failure counts under concurrent attempts and review refunds crossing a policy boundary.


## Profile extension verification

All 50 C# source files passed grammar parsing, and project source/resource paths were checked. A C# compiler, Windows UI and SQL Server are unavailable here, so compilation, SQL migration and runtime execution are not claimed. Seven authentication-guard checks were added to the logic test suite; execute them on Windows.

Windows checks:

1. Upgrade an existing database by launching the rebuilt app; verify existing accounts and bookings remain.
2. Edit a passenger profile, save, navigate away and restart. Verify changes persist without changing booked ticket names.
3. Upload JPG/PNG, save, restart, remove and save. Reject malformed files and files over 2 MB.
4. Submit a reset request twice; only one Pending request should be saved. Check status after refresh.
5. As admin, reject one request and reset another. Verify Approved status and one audit record per reset.
6. Reset a passenger who has no request. Verify the old password fails and the temporary password works.
7. At next sign-in, close the mandatory change dialog: the passenger must remain signed out. Try mismatched, weak, wrong-current and unchanged passwords. Save a valid new password and verify access and subsequent login.
8. Confirm passenger accounts cannot open admin security operations and unauthenticated service calls are denied.
