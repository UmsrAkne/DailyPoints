using System;

namespace DailyPoints.Databases
{
    public class PointTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Type { get; set; } = "Addition";

        public DateTime Date { get; set; } = DateTime.Now;

        public int Points { get; set; }

        public virtual PointSourceDetails Details { get; set; }

        public string HeaderText
        {
            get
            {
                if (Details == null)
                {
                    return string.Empty;
                }

                if (Details.MoneyExpense != null)
                {
                    return Type;
                }

                return Details.TaskItem.IssueId;
            }
        }
    }
}