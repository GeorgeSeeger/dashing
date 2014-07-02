namespace Dashing {
    using System;
    using System.Collections.Generic;
    using System.Data;
using System.Linq.Expressions;

    /// <summary>
    ///     The Session interface.
    /// </summary>
    public interface ISession : IDisposable {
        /// <summary>
        ///     Gets the connection.
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        ///     Gets the transaction.
        /// </summary>
        IDbTransaction Transaction { get; }

        /// <summary>
        ///     The complete.
        /// </summary>
        void Complete();

        T Get<T>(int id, bool? asTracked = null);

        T Get<T>(Guid id, bool? asTracked = null);

        IEnumerable<T> Get<T>(IEnumerable<int> ids, bool? asTracked = null);

        IEnumerable<T> Get<T>(IEnumerable<Guid> ids, bool? asTracked = null);

        /// <summary>
        ///     The query.
        /// </summary>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="SelectQuery" />.
        /// </returns>
        ISelectQuery<T> Query<T>();

        /// <summary>
        ///     The insert.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Insert<T>(params T[] entities);

        /// <summary>
        ///     The insert.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Insert<T>(IEnumerable<T> entities);

        /// <summary>
        ///     The update.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Update<T>(params T[] entities);

        /// <summary>
        ///     The update.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Update<T>(IEnumerable<T> entities);

        /// <summary>
        ///     The delete.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Delete<T>(params T[] entities);

        /// <summary>
        ///     The delete.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        ///     The <see cref="int" />.
        /// </returns>
        int Delete<T>(IEnumerable<T> entities);

        void UpdateAll<T>(Action<T> update);

        void Update<T>(Action<T> update, Expression<Func<T, bool>> predicate);

        void Update<T>(Action<T> update, IEnumerable<Expression<Func<T, bool>>> predicates);

        void Update<T>(Action<T> update, params Expression<Func<T, bool>>[] predicates);

        void DeleteAll<T>();

        void Delete<T>(Expression<Func<T, bool>> predicate);

        void Delete<T>(IEnumerable<Expression<Func<T, bool>>> predicates);

        void Delete<T>(params Expression<Func<T, bool>>[] predicates);
    }
}