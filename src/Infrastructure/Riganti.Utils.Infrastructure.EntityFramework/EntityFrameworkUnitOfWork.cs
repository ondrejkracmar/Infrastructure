using Riganti.Utils.Infrastructure.Core;
using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace Riganti.Utils.Infrastructure.EntityFramework
{
    /// <summary>
    /// An implementation of unit of work in Entity Framework.
    /// </summary>
    public class EntityFrameworkUnitOfWork : EntityFrameworkUnitOfWork<DbContext>
    {
        public EntityFrameworkUnitOfWork(IUnitOfWorkProvider unitOfWorkProvider, Func<DbContext> dbContextFactory, DbContextOptions options)
            : base(unitOfWorkProvider, dbContextFactory, options)
        {
        }

        /// <summary>
        /// Tries to get the <see cref="DbContext" /> in the current scope.
        /// </summary>
        public static DbContext TryGetDbContext(IUnitOfWorkProvider unitOfWorkProvider)
        {
            return TryGetDbContext<DbContext>(unitOfWorkProvider);
        }

        /// <summary>
        /// Tries to get the <see cref="DbContext" /> in the current scope.
        /// </summary>
        public static TDbContext TryGetDbContext<TDbContext>(IUnitOfWorkProvider unitOfWorkProvider)
            where TDbContext : DbContext
        {
            var index = 0;
            var uow = unitOfWorkProvider.GetCurrent(index);
            while (uow != null)
            {
                if (uow is EntityFrameworkUnitOfWork<TDbContext> efuow)
                {
                    return efuow.Context;
                }

                index++;
                uow = unitOfWorkProvider.GetCurrent(index);
            }

            return null;
        }
    }

    /// <summary>
    /// An implementation of unit of work in Entity ramework.
    /// </summary>
    public class EntityFrameworkUnitOfWork<TDbContext> : UnitOfWorkBase, ICheckChildCommitUnitOfWork
        where TDbContext : DbContext
    {
        private readonly bool hasOwnContext;

        /// <summary>
        /// Gets the <see cref="DbContext" />.
        /// </summary>
        public TDbContext Context { get; }

        /// <inheritdoc cref="ICheckChildCommitUnitOfWork.Parent" />
        public IUnitOfWork Parent { get; }

        /// <inheritdoc cref="ICheckChildCommitUnitOfWork.CommitPending" />
        public bool CommitPending { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkUnitOfWork{TDbContext}" /> class.
        /// </summary>
        public EntityFrameworkUnitOfWork(IEntityFrameworkUnitOfWorkProvider<TDbContext> unitOfWorkProvider, Func<TDbContext> dbContextFactory, DbContextOptions options)
            : this((IUnitOfWorkProvider)unitOfWorkProvider, dbContextFactory, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFrameworkUnitOfWork{TDbContext}" /> class.
        /// </summary>
        protected EntityFrameworkUnitOfWork(IUnitOfWorkProvider unitOfWorkProvider, Func<TDbContext> dbContextFactory, DbContextOptions options)
        {
            Parent = unitOfWorkProvider.GetCurrent();

            if (options == DbContextOptions.ReuseParentContext)
            {
                var parentContext = EntityFrameworkUnitOfWork.TryGetDbContext<TDbContext>(unitOfWorkProvider);
                if (parentContext != null)
                {
                    Context = parentContext;
                    return;
                }
            }

            Context = dbContextFactory();
            hasOwnContext = true;
        }

        /// <summary>
        /// Commits this instance when we have to. Skip and request from parent, if we don't own the context.
        /// </summary>
        public override void Commit()
        {
            if (HasOwnContext())
            {
                CommitPending = false;
                base.Commit();
            }
            else
            {
                TryRequestParentCommit();
            }
        }
        /// <summary>
        /// Commits this instance when we have to. Skip and request from parent, if we don't own the context.
        /// </summary>
        public override Task CommitAsync(CancellationToken cancellationToken)
        {
            if (HasOwnContext())
            {
                CommitPending = false;
                return base.CommitAsync(cancellationToken);
            }

            TryRequestParentCommit();

            return Task.CompletedTask;
        }

        public override async Task CommitAsync()
        {
            await CommitAsync(default(CancellationToken));
        }

        /// <inheritdoc cref="ICheckChildCommitUnitOfWork.RequestCommit" />
        public void RequestCommit()
        {
            CommitPending = true;
        }

        /// <summary>
        /// Commits the changes.
        /// </summary>
        protected override void CommitCore()
        {
            Context.SaveChanges();
        }

        protected override async Task CommitAsyncCore(CancellationToken cancellationToken)
        {
            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Disposes the context.
        /// </summary>
        protected override void DisposeCore()
        {
            if (HasOwnContext())
            {
                Context.Dispose();

                if (CommitPending)
                {
                    throw new ChildCommitPendingException();
                }
            }
            else if (CommitPending)
            {
                TryRequestParentCommit();
            }
        }

        private void TryRequestParentCommit()
        {
            if (Parent is ICheckChildCommitUnitOfWork uow)
            {
                uow.RequestCommit();
            }
        }

        private bool HasOwnContext()
        {
            return hasOwnContext;
        }
    }
}