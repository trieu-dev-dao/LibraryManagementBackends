# Read-only local inventory reconciliation — 2026-11-16

Source: `LibraryManagementDb` on the local SQL Express instance, queried read-only. Applied migrations: `InitialCreate`, `ExpiredDate`, `OwnedCopies`. The new `InventoryReconciliation` migration was not applied. `TotalQuantity` is NULL for every book, so none is confirmed. The loan query found no stored status/return-date mismatch, negative available count, or orphaned book reference. Historical stock transitions still prevent an automatic ownership backfill.

| Book ID | Title | Available copies | Observed open loan records | Verified outstanding borrowed copies | Existing total | Status | Proposed total |
|---:|---|---:|---:|---:|---:|---|---:|
| 1 | Doraemon | 10 | 0 | unknown | null | pending | none |
| 2 | Aladin và 40 tên cướp | 1 | 0 | unknown | null | pending | none |
| 5 | Conan | 3 | 1 | unknown | null | pending | none |
| 10 | Clean Architecture | 11 | 1 | unknown | null | pending | none |

**Counts:** verified 0; pending 4; inconsistent 0. The two observed open records must be checked against physical borrowed copies before staff enter verified totals. All four books remain pending in the local database.

## Staff steps

1. Deploy the `InventoryReconciliation` schema migration in the intended environment after backup. No existing `TotalQuantity` value is overwritten by the migration.
2. With an Admin or Librarian Bearer token, call `GET /api/inventory/reconciliation` and review each book. Physically count available copies and confirm each unreturned one-copy loan against the lending ledger. Correct any stock or loan discrepancy before continuing.
3. For each book, send its ID and all three verified counts plus a specific source note to `POST /api/inventory/reconciliation/preview`. For example, book 5 can use the example body in `docs/api-contract.md` **only if** staff independently confirm those counts. A `canApply: false` response explains what needs correction.
4. Send the same body to `POST /api/inventory/reconciliation/apply`. Repeat the report. When `pendingCount` is zero, `GET /api/dashboard/metrics` returns the owned-copy sum. Until then `totalOwnedCopies` remains null and `pendingReconciliationCount` explains the gap.

Example PowerShell after deployment, using verified counts entered by staff (replace the sample values and source for each book):

```powershell
$apiBase = 'https://your-library-server'
$headers = @{ Authorization = "Bearer $staffToken" }
$report = Invoke-RestMethod -Uri "$apiBase/api/inventory/reconciliation" -Headers $headers
$report.books | Format-Table bookId,title,availableCopies,observedOutstandingLoanRecords,verifiedOutstandingBorrowedCopies,status
$body = @{
    bookId = 5
    verifiedAvailableCopies = 3
    verifiedOutstandingBorrowedCopies = 1
    verifiedTotalQuantity = 4
    source = 'physical shelf count + checked loan ledger, 2026-11-16'
} | ConvertTo-Json
$preview = Invoke-RestMethod -Method Post -Uri "$apiBase/api/inventory/reconciliation/preview" -Headers $headers -ContentType 'application/json' -Body $body
$preview
if ($preview.canApply) {
    Invoke-RestMethod -Method Post -Uri "$apiBase/api/inventory/reconciliation/apply" -Headers $headers -ContentType 'application/json' -Body $body
}
```
