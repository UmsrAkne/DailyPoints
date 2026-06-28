using System;

namespace DailyPoints.Databases
{
    public class MoneyExpenseItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Description { get; set; } = string.Empty; // 例: "カフェ代", "ゲーム購入"

        public int Amount { get; set; } // 消費した現実の金額
    }
}