using System;
using DailyPoints.Models;

namespace DailyPoints.Databases
{
    public class PointTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Type { get; set; } = "Addition";

        public DateTime Date { get; set; } = DateTime.Now;

        public int Points { get; set; }

        public TaskItem TaskItem { get; set; }
    }
}