Validation completed on 4 October 2026:

| Check | Passed | Failed | Skipped | Duration |
| --- | ---: | ---: | ---: | --- |
| Complete domain suite | 64 | 0 | 0 | 2 s |
| All affected API/browser/publication/authentication/recovery tests | 111 | 0 | 0 | 4 m 59 s |
| Final migration and rejected-proposal browser checks | 3 | 0 | 0 | 39 s |
| Initial complete integration/browser suite | 294 | 6 | 0 | 17 m 8 s |
| Untouched baseline hydration checks | 0 | 2 | 0 | 1 m 11 s |

The initial full run's four affected failures were corrected and all are included in the successful 111-test run: font-loading timing in scanner navigation measurement, installed passkey submission expectations, installed Backpack legacy-draft expectations, and WebKit navigation away from the development sign-in landing before it settled. The other two failures are the Chromium/WebKit versions of Proposal_does_not_accept_input_until_hydration_and_draft_restore_complete, both timing out at PwaReliabilityTests.cs:172. Both reproduce on untouched GitHub commit 616046e. The complete suite is therefore not reported as all green. Its successful unaffected tests were not repeated.

Final Release solution build: 0 warnings, 0 errors, 4.32 s. Final git diff --check passed. The migration check confirms no pending EF model changes and preserves old proposals, image publication/permissions and exact observations across Down/Up in its disposable PostgreSQL fixture.

Commands used from the repository (Release configuration):
- dotnet build DiaperScout.slnx -c Release --no-restore
- dotnet test tests/DiaperScout.Domain.Tests/DiaperScout.Domain.Tests.csproj -c Release --no-build
- dotnet test DiaperScout.slnx -c Release --settings docs/implementation/search-prototype-evidence/validation.runsettings
- Affected integration filter: FullyQualifiedName~ScanDiscoveryBrowserTests|FullyQualifiedName~PwaScannerBrowserTests|FullyQualifiedName~PlaceObservation|FullyQualifiedName~ExplorerProposalEvidenceTests|FullyQualifiedName~CatalogueApiTests|FullyQualifiedName~PasskeyBrowserTests|FullyQualifiedName~BackpackPersonalBrowserTests
- Final follow-up filter: FullyQualifiedName~ScanDiscoveryBrowserTests|FullyQualifiedName~ExplorerProposalMigrationTests

All integration runs used the serial validation.runsettings, isolated Docker PostgreSQL, D:/Codex/DiaperScout-welcome-temp and D:/Codex/DiaperScout-scan-results. Exact TRX files are scan-final-domain.trx, scan-final-repository.trx, scan-final-migration-and-stale.trx, scan-full-suite.trx and scan-baseline-hydration.trx. The solution-level logger overwrote the initial domain TRX with integration results; the separate final domain run preserves its exact result.

Inspected previews and the file manifest are in scan-discovery-evidence/. Full-page recognition screenshots contain fixed bottom navigation at the viewport boundary; content below it remains scrollable. These are isolated test data, not production screenshots.
