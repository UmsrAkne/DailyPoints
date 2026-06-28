using System;
using DailyPoints.Models;

namespace DailyPoints.Databases
{
    public class PointSourceDetails
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // タスクへの参照
        public Guid? TaskItemId { get; set; }

        public virtual TaskItem TaskItem { get; set; }

        // 金銭消費への参照
        public Guid? MoneyExpenseItemId { get; set; }

        public virtual MoneyExpenseItem MoneyExpense { get; set; }
    }
}