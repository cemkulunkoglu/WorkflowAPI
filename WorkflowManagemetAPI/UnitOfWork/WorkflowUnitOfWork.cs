using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;
using WorkflowManagemetAPI.Interfaces;

namespace WorkflowManagemetAPI.UoW
{
    public class WorkflowUnitOfWork<TEntity> : IUnitOfWork<TEntity> where TEntity : class
    {
        internal Microsoft.EntityFrameworkCore.DbContext context;
        internal DbSet<TEntity> dbSet;
        private bool _disposed;

        public WorkflowUnitOfWork(Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            context = dbContext;
            dbSet = context.Set<TEntity>();
        }

        public IDbContextTransaction BeginTransaction()
        {
            return context.Database.BeginTransaction();
        }

        public void Commit(IDbContextTransaction transaction)
        {
            try
            {
                transaction.Commit();
            }
            catch
            {
                Rollback(transaction);
                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }

        public void Rollback(IDbContextTransaction transaction)
        {
            transaction.Rollback();
            transaction.Dispose();
        }

        public IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        public TEntity? GetByID(object id)
        {
            return dbSet.Find(id);
        }

        public int Count(Expression<Func<TEntity, bool>> filter)
        {
            return dbSet.Where(filter).Count();
        }

        public TEntity Insert(TEntity entity)
        {
            dbSet.Add(entity);
            return entity;
        }

        public List<TEntity> InsertRange(List<TEntity> entityList)
        {
            dbSet.AddRange(entityList);
            return entityList;
        }

        public TEntity Update(TEntity entityToUpdate)
        {
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            return entityToUpdate;
        }

        public IList<TEntity> UpdateAll(IList<TEntity> entityToUpdateList)
        {
            foreach (TEntity item in entityToUpdateList)
            {
                dbSet.Attach(item);
                context.Entry(item).State = EntityState.Modified;
            }
            return entityToUpdateList;
        }

        public void Delete(object id)
        {
            TEntity? entityToDelete = dbSet.Find(id);
            if (entityToDelete != null)
            {
                Delete(entityToDelete);
            }
        }

        public void Delete(TEntity entityToDelete)
        {
            if (context.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
        }

        public IList<TEntity> DeleteAll(IList<TEntity> entityToDeleteList)
        {
            foreach (TEntity item in entityToDeleteList)
            {
                Delete(item);
            }
            return entityToDeleteList;
        }

        public int SaveChanges()
        {
            return context.SaveChanges();
        }

        public Task SaveChangeAsync()
        {
            return context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                context.Dispose();
            }
            _disposed = true;
        }
    }
}