using Dapper;
using Microsoft.EntityFrameworkCore; // Ensure this is the correct using for DbContext
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using WorkflowManagemetAPI.Interfaces;
using WorkflowManagemetAPI.Models;
using WorkflowManagemetAPI.Repositories;
using static Dapper.SqlMapper;

namespace WorkflowManagemetAPI.UoW
{
    public class UnitOfWork<TEntity> : IUnitOfWork<TEntity>, IDisposable where TEntity : class
    {
		internal Microsoft.EntityFrameworkCore.DbContext context;
		internal DbSet<TEntity> dbSet;
		private bool _disposed;

		public UnitOfWork(Microsoft.EntityFrameworkCore.DbContext dbContext)
		{
			context = dbContext;
			dbSet = context.Set<TEntity>();
		}

		public void Delete(object id)
		{
			TEntity? entityToDelete = dbSet.Find(id);
			Delete(entityToDelete);
		}

		public void Delete(TEntity? entityToDelete)
		{
			if (entityToDelete != null)
			{
				if (context.Entry(entityToDelete).State == EntityState.Detached)
				{
					dbSet.Attach(entityToDelete);
				}
				dbSet.Remove(entityToDelete);
			}
		}


		public Task SaveChangeAsync()
		{
			return context.SaveChangesAsync();
		}

		public int SaveChanges()
		{
			return context.SaveChanges();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				context.Dispose();
			}
			_disposed = true;
		}
		public IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>>? filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, string includeProperties = "")
		{
			IQueryable<TEntity> query = dbSet;

			if (filter != null)
			{
				query = query.Where(filter);
			}

			foreach (var includeProperty in includeProperties.Split
				(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
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

		public TEntity Insert(TEntity entity)
		{
			dbSet.Add(entity);
			return entity;
		}

		public TEntity Update(TEntity entityToUpdate)
		{
			dbSet.Attach(entityToUpdate);
			context.Entry(entityToUpdate).State = EntityState.Modified;
			return entityToUpdate;
		}


		public int Count(Expression<Func<TEntity, bool>> filter)
		{
			return dbSet.Where(filter).Count();
		}

		public int Count(IQueryable<TEntity> query)
		{
			return query.Count();
		}

		private readonly MethodInfo OrderByMethod = typeof(Queryable).GetMethods().Single(method => method.Name == "OrderBy" && method.GetParameters().Length == 2);

		private readonly MethodInfo OrderByDescendingMethod = typeof(Queryable).GetMethods().Single(method => method.Name == "OrderByDescending" && method.GetParameters().Length == 2);

		public bool PropertyExists<T>(string propertyName)
		{
			return typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) != null;
		}

		public IQueryable<T>? OrderByProperty<T>(IQueryable<T> source, string propertyName)
		{
			if (typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) == null)
			{
				return null;
			}
			ParameterExpression paramterExpression = Expression.Parameter(typeof(T));
			Expression orderByProperty = Expression.Property(paramterExpression, propertyName);
			LambdaExpression lambda = Expression.Lambda(orderByProperty, paramterExpression);
			MethodInfo genericMethod = OrderByMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
			object? ret = genericMethod.Invoke(null, new object[] { source, lambda });
			return (IQueryable<T>?)ret;
		}

		public IQueryable<T>? OrderByPropertyDescending<T>(IQueryable<T> source, string propertyName)
		{
			if (typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) == null)
			{
				return null;
			}
			ParameterExpression paramterExpression = Expression.Parameter(typeof(T));
			Expression orderByProperty = Expression.Property(paramterExpression, propertyName);
			LambdaExpression lambda = Expression.Lambda(orderByProperty, paramterExpression);
			MethodInfo genericMethod = OrderByDescendingMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
			object? ret = genericMethod.Invoke(null, new object[] { source, lambda });
			return (IQueryable<T>?)ret;
		}

		public List<TEntity> InsertRange(List<TEntity> entity)
		{
			dbSet.AddRange(entity);
			return entity;
		}

		public IList<TEntity> DeleteAll(IList<TEntity> entityToDeleteList)
		{
			foreach (TEntity item in entityToDeleteList)
			{
				if (item != null)
				{
					if (context.Entry(item).State == EntityState.Detached)
					{
						dbSet.Attach(item);
					}
					dbSet.Remove(item);
				}
			}
			return entityToDeleteList;
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
	}
}
