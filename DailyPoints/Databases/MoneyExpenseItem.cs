using System;
using System.Text.Json.Serialization;

namespace DailyPoints.Databases
{
    public class MoneyExpenseItem
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty; // 例: "カフェ代", "ゲーム購入"

        [JsonPropertyName("amount")]
        public int Amount { get; set; } // 消費した現実の金額
    }
}