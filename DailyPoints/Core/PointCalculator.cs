using System.Collections.Generic;
using System.Linq;
using DailyPoints.Databases;
using DailyPoints.Models;

namespace DailyPoints.Core
{
    public class PointCalculator
    {
        public int Calculate(IEnumerable<TaskItem> items)
        {
            return items.Sum(taskItem => Calculate(taskItem).Points);
        }

        public PointTransaction Calculate(TaskItem item)
        {
            var pt = new PointTransaction
            {
                TaskItem = item,
            };

            pt.Points += (int)(item.ActualTime.TotalMinutes * 1.1) * 10;
            pt.Points += (int)item.Estimation.TotalMinutes * 10;
            return pt;
        }

        public PointTransaction Deduct(TaskItem item)
        {
            var pt = new PointTransaction
            {
                Type = "Deduction",
                Points = -((int)item.ActualTime.TotalMinutes * 9),
                TaskItem = item,
            };

            return pt;
        }

        public PointTransaction Deduct(MoneyExpenseItem item)
        {
            var pt = new PointTransaction
            {
                Type = "MoneyExpense",
                Points = -item.Amount,
                MoneyExpenseItem = item,
            };

            return pt;
        }
    }
}