using System.Data;
using System.Linq.Expressions;
using WorkflowManagemetAPI.Repositories;

public interface IUnitOfWork<TEntity> where TEntity : class
{
	public abstract IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>>? filter = null,
		Func<IQueryable<TEntity>,
		IOrderedQueryable<TEntity>>? orderBy = null, string includeProperties = "");
	public abstract TEntity? GetByID(object id);
	public abstract TEntity Insert(TEntity entity);
	public abstract List<TEntity> InsertRange(List<TEntity> entity);
	public abstract void Delete(object id);
	public abstract void Delete(TEntity entityToDelete);
	public abstract TEntity Update(TEntity entityToUpdate);
	public int Count(Expression<Func<TEntity, bool>> filter);
	public int Count(IQueryable<TEntity> query);
	public bool PropertyExists<T>(string propertyName);
	public IQueryable<T> OrderByProperty<T>(IQueryable<T> source, string propertyName);
	public IQueryable<T> OrderByPropertyDescending<T>(IQueryable<T> source, string propertyName);
	public int SaveChanges();
	public Task SaveChangeAsync();
	public void Dispose();
	public IList<TEntity> DeleteAll(IList<TEntity> entityToDelete);
	public IList<TEntity> UpdateAll(IList<TEntity> entityToUpdate);
}
