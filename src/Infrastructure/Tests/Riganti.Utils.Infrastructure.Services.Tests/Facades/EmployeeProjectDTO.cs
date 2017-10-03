using Riganti.Utils.Infrastructure.Core;

namespace Riganti.Utils.Infrastructure.Services.Tests.Facades
{
    public class EmployeeProjectDTO : IEntity<int>
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int ProjectId { get; set; }
    }
}