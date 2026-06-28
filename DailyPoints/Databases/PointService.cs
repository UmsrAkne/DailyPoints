using System.Collections.Generic;

namespace DailyPoints.Databases
{
    public class PointService
    {
        private readonly IRepository repository;

        public PointService(IRepository repository)
        {
            this.repository = repository;
        }

        public void AddRange(IEnumerable<PointTransaction> pointTransactions)
        {
            repository.AddRange(pointTransactions);
        }

        public void Add(PointTransaction pointTransactions)
        {
            repository.Add(pointTransactions);
        }

        public IEnumerable<PointTransaction> GetAll()
        {
            return repository.GetAll();
        }
    }
}