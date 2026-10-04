# Production deployment — 4 October 2026

User authorised commit and deployment. Runtime commit: `3fd804d0d762a6e7a1186fe418dc343841036d76`, pushed to `fix/product-submission-recovery`. Images were built from an immutable archive of that exact commit. Subsequent evidence-only commits do not change deployed code.

## Release

ACR builds all succeeded: API `db1y`, Web `db1x`, migrations `db1w`.

Registry: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io`.

| Image | Deployed digest |
| --- | --- |
| diaperscout-api | sha256:cc5150c6a53d3802eac293715006ea177c907f53bc97623121d3246328a0049b |
| diaperscout-web | sha256:a1d9f2da01185d0526f2c7f3262cbe06d4db3ec07f63a39b53397159312566f2 |
| diaperscout-migrations | sha256:2032bf3d1226a873b6cc8ba76cb8744d58408dad8e47108f152a8fc96f419582 |

Existing private job execution `ds-bootstrap-admin-33laro0` used an execution-only migration image override, its image's `/app/efbundle` entrypoint, and no bootstrap arguments. It succeeded between 10:18:30 and 10:19:04 UTC, applying `20261004085541_ExplorerProposalEvidence`. See [migration log](migration.log). The persistent job template was preserved.

API revision `diaperscout-api-vnet--scan-3fd804d` and Web revision `diaperscout-web-vnet--scan-3fd804d` were both Healthy and Provisioned with 100% traffic. Existing identities, configuration, environment, workload profiles and container templates were preserved except the intended app image/revision suffix changes: [comparison](config-comparison.json). Secret values and private configuration snapshots are excluded from this evidence.

## Live verification

[Production smoke results](production-smoke.json): Chromium 390×844 and WebKit 430×932 both passed. Public Explore, Products, Atlas, Backpack and Scan returned 200. Installed unknown-barcode entry and photo-entry sign-in gating rendered without horizontal overflow or browser errors. Sign-in retained the barcode return destination; anonymous private image access redirected to authentication (302). Screenshots are alongside the report. No production contribution or catalogue records were created.

Authenticated known-product discovery, evidence upload and moderator resolution were verified by isolated automated tests, not by writing synthetic production data. Real iPhone camera/permission behaviour remains unverified. Repository test totals and the two inherited complete-suite hydration timeouts are recorded in [validation](validation.md).

## Rollback

Restore prior app images if needed; retain the additive database migration rather than running a production Down migration.

- API: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api@sha256:766c7d588508da19b8fb5812c97812e00a6cfed25999cefac610a35dd4c94abb`
- Web: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:4b93b157cea80b2f22b125909c68d0b1e3bbcc4c920aa58bd9bf0199ee017261`
