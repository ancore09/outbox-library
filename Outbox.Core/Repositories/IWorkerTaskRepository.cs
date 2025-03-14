using System.Data;
using Outbox.Abstractions.Models;

namespace Outbox.Core.Repositories;

public interface IWorkerTaskRepository
{
    Task BeginTransaction();
    void Commit();
    void Rollback();


    Task<WorkerTask?> GetFirstFreeTaskWithLock();
    Task UpdateLease(long id, DateTimeOffset leaseEnd);
    Task UpdateLeases(List<(WorkerTask WorkerTask, DateTimeOffset LeaseEnd)> updateQuery);

}