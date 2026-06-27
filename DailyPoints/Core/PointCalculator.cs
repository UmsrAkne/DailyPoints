using System.Collections.Generic;
using DailyPoints.Models;

namespace DailyPoints.Core
{
    public class PointCalculator
    {
        public int Calculate(IEnumerable<TaskItem> items)
        {
            var total = 0;
            foreach (var taskItem in items)
            {
                total += (int)(taskItem.ActualTime.TotalMinutes * 1.1) * 10;
                total += (int)taskItem.Estimation.TotalMinutes * 10;
            }

            return total;
        }
    }
}