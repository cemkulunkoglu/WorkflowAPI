using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
public interface IWorkflowUnitOfWork<TEntity> : IDisposable where TEntity : class
{
    IEnumerable<TEntity> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "");

    TEntity? GetByID(object id);

    int Count(Expression<Func<TEntity, bool>> filter);
    TEntity Insert(TEntity entity);
    List<TEntity> InsertRange(List<TEntity> entity);

    TEntity Update(TEntity entityToUpdate);
    IList<TEntity> UpdateAll(IList<TEntity> entityToUpdate);

    void Delete(object id);
    void Delete(TEntity entityToDelete);
    IList<TEntity> DeleteAll(IList<TEntity> entityToDelete);

    int SaveChanges();
    Task SaveChangeAsync();

    IDbContextTransaction BeginTransaction();       // "Kaydı başlat"
    void Commit(IDbContextTransaction transaction); // "Her şeyi onayla"
    void Rollback(IDbContextTransaction transaction); // "Hata var, geri al"
}