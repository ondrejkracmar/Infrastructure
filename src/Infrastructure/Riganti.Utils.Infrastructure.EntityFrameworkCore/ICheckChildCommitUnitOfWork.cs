using Riganti.Utils.Infrastructure.Core;

namespace Riganti.Utils.Infrastructure.EntityFrameworkCore
{
    /// <summary>
    /// Prescription of this interface is used to track children commit requests.
    /// Such unit of work
    /// </summary>
    public interface ICheckChildCommitUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Parent UnitOfWork who is the source of reused resources.
        /// </summary>
        IUnitOfWork Parent { get; }

        /// <summary>
        /// Flag used to track the children commit requests.
        /// </summary>
        bool CommitPending { get; }

        /// <summary>
        /// Called by child unit of work with reused resources on commit.
        /// </summary>
        void RequestCommit();
    }
}