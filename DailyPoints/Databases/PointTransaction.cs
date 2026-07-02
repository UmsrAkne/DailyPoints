using System;
using System.Text.Json.Serialization;
using DailyPoints.Models;

namespace DailyPoints.Databases
{
    public class PointTransaction
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("category")]
        public string Type { get; set; } = "Addition";

        [JsonPropertyName("date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("task_item")]
        public TaskItem TaskItem { get; set; }

        [JsonPropertyName("money_expense_item")]
        public MoneyExpenseItem MoneyExpenseItem { get; set; }

        [JsonIgnore]
        public string HeaderText
        {
            get
            {
                if (TaskItem == null && MoneyExpenseItem == null)
                {
                    return string.Empty;
                }

                return TaskItem != null
                    ? TaskItem.IssueId
                    : MoneyExpenseItem.Description;
            }
        }
    }
}