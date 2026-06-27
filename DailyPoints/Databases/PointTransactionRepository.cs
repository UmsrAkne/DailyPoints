using System.Collections.Generic;

namespace DailyPoints.Databases
{
    public class PointTransactionRepository : IRepository
    {
        private readonly List<PointTransaction> transactions = new List<PointTransaction>();

        public IEnumerable<PointTransaction> GetAll()
        {
            return transactions;
        }

        public void AddRange(IEnumerable<PointTransaction> pointTransactions)
        {
            transactions.AddRange(pointTransactions);
        }

        public void Add(PointTransaction pointTransactions)
        {
            transactions.Add(pointTransactions);
        }
    }
}