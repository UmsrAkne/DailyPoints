using System.Collections.Generic;

namespace DailyPoints.Databases
{
    public interface IRepository
    {
        IEnumerable<PointTransaction> GetAll();

        void AddRange(IEnumerable<PointTransaction> pointTransactions);

        void Add(PointTransaction pointTransactions);
    }
}