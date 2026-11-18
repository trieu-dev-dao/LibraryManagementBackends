using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.DTOs;
using System.Text.Json;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception($"Failed: {scenario}");
    Console.WriteLine($"PASS {scenario}");
}

var due = new DateTime(2026, 11, 16);
var submitted = JsonSerializer.Deserialize<BorrowRecordCreateDto>(
    "{\"bookId\":1,\"memberId\":1,\"expiredDate\":\"2026-11-16T23:00:00-05:00\"}",
    new JsonSerializerOptions(JsonSerializerDefaults.Web));
Check(submitted?.ExpiredDate == due, "due input preserves submitted calendar date despite offset");
var loan = new BorrowRecord { ExpiredDate = due, ReturnDate = null, Status = LoanRules.Borrowing };
Check(LoanRules.Status(loan) == LoanRules.Borrowing,
    "due date remains valid through its end");
Check(LoanRules.IsCurrentlyOverdue(loan, new DateOnly(2026, 11, 17)) &&
      LoanRules.Status(loan) == LoanRules.Borrowing,
    "unreturned loan past its due date remains Borrowing");
Check(LoanRules.IsCurrentlyOverdue(loan, new DateOnly(2026, 11, 17)),
    "current overdue predicate includes an unreturned loan");
Check(LoanRules.BusinessDate(new DateTime(2026, 11, 16, 16, 59, 59, DateTimeKind.Utc)) == new DateOnly(2026, 11, 16),
    "Vietnam date before UTC boundary");
Check(LoanRules.BusinessDate(new DateTime(2026, 11, 16, 17, 0, 0, DateTimeKind.Utc)) == new DateOnly(2026, 11, 17),
    "Vietnam date at UTC boundary");
loan.ReturnDate = new DateTime(2026, 11, 16, 16, 59, 59, DateTimeKind.Utc);
Check(LoanRules.Status(loan) == LoanRules.Returned && !LoanRules.IsLateReturn(loan),
    "return through end of due date is Returned");
loan.ReturnDate = new DateTime(2026, 11, 16, 17, 0, 0, DateTimeKind.Utc);
Check(LoanRules.Status(loan) == LoanRules.Overdue && LoanRules.IsLateReturn(loan),
    "return at start of next business date displays Overdue");
Check(LoanRules.StoredStatus(loan) == LoanRules.Returned,
    "late return remains stored as completed");
Check(!LoanRules.IsCurrentlyOverdue(loan, new DateOnly(2026, 11, 17)),
    "late return is excluded from current overdue count");

var returnBook = new Book { Quantity = 2 };
var returnLoan = new BorrowRecord { ReturnDate = null };
LoanRules.EnsureReturnable(returnLoan);
InventoryRules.Return(returnBook);
returnLoan.ReturnDate = new DateTime(2026, 11, 17, 0, 0, 0, DateTimeKind.Utc);
var repeatRejected = false;
try { LoanRules.EnsureReturnable(returnLoan); InventoryRules.Return(returnBook); }
catch (InvalidOperationException) { repeatRejected = true; }
Check(repeatRejected && returnBook.Quantity == 3,
    "repeated return is rejected before a second stock increment");

var book = new Book { Quantity = 3, TotalQuantity = 4 };
InventoryRules.Lend(book);
Check(book.Quantity == 2 && book.TotalQuantity == 4, "lending changes availability only");
InventoryRules.Return(book);
Check(book.Quantity == 3 && book.TotalQuantity == 4, "returning changes availability only");
Check(InventoryRules.OwnedAfterAvailableAdjustment(4, 3, 5, 1) == 6,
    "inventory addition changes owned count by added copies");

var localBooks = new[]
{
    (Id: 1, Title: "Doraemon", Available: 10, Open: 0),
    (Id: 2, Title: "Aladin và 40 tên cướp", Available: 1, Open: 0),
    (Id: 5, Title: "Conan", Available: 3, Open: 1),
    (Id: 10, Title: "Clean Architecture", Available: 11, Open: 1)
};
var localRows = localBooks.Select(x => InventoryReconciliationRules.Assess(
    new Book { Id = x.Id, Title = x.Title, Quantity = x.Available, TotalQuantity = null }, x.Open, 0)).ToList();
Check(localRows.Count(r => r.Status == "pending") == 4 &&
      localRows.All(r => r.VerifiedOutstandingBorrowedCopies is null),
    "read-only local snapshot remains pending with no guessed borrowed copies");
Check(InventoryReconciliationRules.VerifiedOwnedSum(localRows) is null,
    "owned-copy dashboard metric stays unavailable while local books are pending");

var confirmedZero = new Book
{
    Id = 20, Quantity = 0, TotalQuantity = 0,
    TotalQuantitySource = "physical count", TotalQuantityVerifiedAtUtc = DateTime.UtcNow
};
Check(InventoryReconciliationRules.Assess(confirmedZero, 0, 0).Status == "verified",
    "confirmed zero is preserved as verified");
Check(InventoryReconciliationRules.VerifiedOwnedSum(
    [InventoryReconciliationRules.Assess(confirmedZero, 0, 0)]) == 0,
    "verified zero contributes zero to the published total");
var unverifiedZero = new Book { Id = 21, Quantity = 0, TotalQuantity = 0 };
Check(InventoryReconciliationRules.Assess(unverifiedZero, 0, 0).Status == "pending",
    "zero without provenance stays pending");
var badTotal = new Book { Id = 22, Quantity = 2, TotalQuantity = 0 };
Check(InventoryReconciliationRules.Assess(badTotal, 1, 0).Status == "inconsistent",
    "unverified default conflicting with stock is flagged inconsistent");
var input = new InventoryReconciliationInputDto
{
    BookId = 5, VerifiedAvailableCopies = 3, VerifiedOutstandingBorrowedCopies = 1,
    VerifiedTotalQuantity = 4, Source = "physical shelf count and checked loan ledger"
};
Check(InventoryReconciliationRules.Validate(input,
    new Book { Id = 5, Quantity = 3 }, 1, 0) is null,
    "matching staff verification is accepted");
input.VerifiedTotalQuantity = 3;
Check(InventoryReconciliationRules.Validate(input,
    new Book { Id = 5, Quantity = 3 }, 1, 0) is not null,
    "total below verified available plus borrowed is rejected");
input.VerifiedTotalQuantity = 4;
input.VerifiedOutstandingBorrowedCopies = 0;
Check(InventoryReconciliationRules.Validate(input,
    new Book { Id = 5, Quantity = 3 }, 1, 0) is not null,
    "stale borrowed count is rejected");
input.VerifiedOutstandingBorrowedCopies = 1;
input.VerifiedAvailableCopies = 2;
Check(InventoryReconciliationRules.Validate(input,
    new Book { Id = 5, Quantity = 3 }, 1, 0) is not null,
    "stale available count is rejected");
input.VerifiedAvailableCopies = 3;
Check(InventoryReconciliationRules.Validate(input,
    new Book { Id = 5, Quantity = 3 }, 1, 1) is not null,
    "contradictory loan status blocks verification");
