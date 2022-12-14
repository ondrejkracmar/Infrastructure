using Riganti.Utils.Infrastructure.Core;
using Xunit;

#if EFCORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

#if EFCORE
namespace Riganti.Utils.Infrastructure.EntityFrameworkCore.Tests.Repository
#else
namespace Riganti.Utils.Infrastructure.EntityFramework.Tests.Repository
#endif
{
    public class EntityFrameworkUnitOfProviderTests
    {

        public class CustomDbContext1 : DbContext
        {
        }

        public class CustomDbContext2 : DbContext
        {
        }

        [Fact]
        public void NestedDbContexts()
        {
            var registry = new ThreadLocalUnitOfWorkRegistry();
            var uowp1 = new EntityFrameworkUnitOfWorkProvider<CustomDbContext1>(registry, () => new CustomDbContext1());
            var uowp2 = new EntityFrameworkUnitOfWorkProvider<CustomDbContext2>(registry, () => new CustomDbContext2());

            using (var uow1 = uowp1.Create())
            {
                using (var uow2 = uowp2.Create())
                {
                    var current = registry.GetCurrent(0);
                    Assert.Equal(uow2, current);

                    var parent = registry.GetCurrent(1);
                    Assert.Equal(uow1, parent);

                    var inner = EntityFrameworkUnitOfWork.TryGetDbContext<CustomDbContext2>(uowp2);
                    Assert.Equal(((EntityFrameworkUnitOfWork<CustomDbContext2>)uow2).Context, inner);

                    var outer = EntityFrameworkUnitOfWork.TryGetDbContext<CustomDbContext1>(uowp1);
                    Assert.Equal(((EntityFrameworkUnitOfWork<CustomDbContext1>)uow1).Context, outer);
                }
            }
        }

    }
}
