using LibraryManagement.Models;

namespace LibraryManagement.Services;

public static class InventoryRules
{
    public static void Lend(Book book)
    {
        if (book.Quantity <= 0) throw new InvalidOperationException("Sách đã hết, không thể mượn.");
        book.Quantity--;
    }

    public static void Return(Book book) => book.Quantity = checked(book.Quantity + 1);

    public static int OwnedAfterAvailableAdjustment(int owned, int oldAvailable, int newAvailable, int outstanding)
    {
        if ((long)owned != (long)oldAvailable + outstanding)
            throw new InvalidOperationException("Tồn kho hiện tại không khớp số bản sở hữu; cần đối soát trước khi điều chỉnh.");
        var next = (long)owned + newAvailable - oldAvailable;
        if (next < 0 || next > int.MaxValue)
            throw new InvalidOperationException("Số bản sở hữu sau điều chỉnh không hợp lệ.");
        return (int)next;
    }
}
