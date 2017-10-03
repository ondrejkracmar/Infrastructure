using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Riganti.Utils.Infrastructure.AutoMapper;
using Riganti.Utils.Infrastructure.Core;
using Riganti.Utils.Infrastructure.EntityFrameworkCore;
using Riganti.Utils.Infrastructure.Services.Facades;
using Xunit;

namespace Riganti.Utils.Infrastructure.Services.Tests.Facades
{
    public class TemporalRelationshipCrudFacadeTests
    {
        [Fact]
        public void GetDetail_Mocks_ForDebug()
        {
            // Arrange
            var employeeProject = new EmployeeProject
            {
                Id = 1,
                EmployeeId = 2,
                ProjectId = 3,
                ValidityBeginDate = DateTime.Today.AddDays(-1),
                ValidityEndDate = DateTime.Today.AddDays(1)
            };

            var employee = new Employee
            {
                Id = 2,
                EmployeeProjects = new List<EmployeeProject> { employeeProject }
            };

            Func <IFilteredQuery<EmployeeProjectDTO, EmployeeProjectFilterDTO>> queryFactory = () => new Mock<IFilteredQuery<EmployeeProjectDTO, EmployeeProjectFilterDTO>>().Object;
            var entityMapper = new Mock<IEntityDTOMapper<EmployeeProject, EmployeeProjectDTO>>().Object;
            var respositoryMock = new Mock<IRepository<EmployeeProject, int>>();
            respositoryMock.Setup(r => r.GetById(It.IsAny<int>(), It.IsAny<Expression<Func<EmployeeProject, object>>[]>())).Returns<int, Expression<Func<EmployeeProject, object>>[]>((id, x) => employeeProject);
            var parentRepositoryMock = new Mock<IRepository<Employee, int>>();
            parentRepositoryMock.Setup(r => r.GetById(It.IsAny<int>())).Returns<int>(id => employee);
            var dateTimeProvider = new LocalDateTimeProvider();
            var unitOfWorkProviderMock = new Mock<IUnitOfWorkProvider>();
            unitOfWorkProviderMock.Setup(p => p.Create()).Returns(() => new Mock<IUnitOfWork>().Object);
            var facade = new EmployeeProjectsFacade(queryFactory, entityMapper, respositoryMock.Object, parentRepositoryMock.Object, dateTimeProvider, unitOfWorkProviderMock.Object);

            // Act - only for debug
            facade.GetDetail(1);     
            
        }

        [Fact]
        public void GetDetail_Returns_CorrectValue()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EmployeeProjectDbContext>()
                .UseInMemoryDatabase(databaseName: "EmployeesAndProjects" + Guid.NewGuid().ToString().Substring(10))
                .Options;
            PrepareInMemoryDbContext(options);
            var facade = GetFacade(options);

            // Act
            var detail = facade.GetDetail(100);

            // Assert
            Assert.NotNull(detail);
            Assert.Equal(100, detail.Id);
        }

        [Fact]
        public void Save_Invalidates_PreviousRelationship()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EmployeeProjectDbContext>()
                .UseInMemoryDatabase(databaseName: "EmployeesAndProjects" + Guid.NewGuid().ToString().Substring(10))
                .Options;
            PrepareInMemoryDbContext(options);
            var facade = GetFacade(options);
            var employeeId = 1; // has only one project
            EmployeeProject employeeProject;
            // get current EmployeeProject for the employee
            using (var ctx = new EmployeeProjectDbContext(options))
            {
                employeeProject = ctx.EmployeeProjects.Single(ep => ep.EmployeeId == employeeId);
            }
            var employeeProjectDTO = Mapper.Map<EmployeeProjectDTO>(employeeProject);
            
            // Act
            var savedDTO = facade.Save(employeeProjectDTO);

            // Assert
            List<EmployeeProject> employeeProjects;
            using (var ctx = new EmployeeProjectDbContext(options))
            {
                employeeProjects = ctx.EmployeeProjects.Where(ep => ep.EmployeeId == employeeId).ToList();
            }
            Assert.Equal(2, employeeProjects.Count);
            var oldRelationship = employeeProjects.SingleOrDefault(ep => ep.Id == employeeProject.Id);
            var newRelationship = employeeProjects.SingleOrDefault(ep => ep.Id != employeeProject.Id);
            Assert.NotNull(oldRelationship);
            Assert.NotNull(newRelationship);
            Assert.True(oldRelationship.ValidityEndDate == newRelationship.ValidityBeginDate);
            Assert.True(newRelationship.ValidityBeginDate < DateTime.Now);
        }

        private static EmployeeProjectsFacade GetFacade(DbContextOptions<EmployeeProjectDbContext> options)
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<EmployeeProjectDTO, EmployeeProject>();
                cfg.CreateMap<EmployeeProject, EmployeeProjectDTO>();
            });

            var dateTimeProvider = new LocalDateTimeProvider();
            IUnitOfWorkProvider unitOfWorkProvider = new EntityFrameworkUnitOfWorkProvider(new AsyncLocalUnitOfWorkRegistry(), () => new EmployeeProjectDbContext(options));
            Func<IFilteredQuery<EmployeeProjectDTO, EmployeeProjectFilterDTO>> queryFactory = () => new Mock<IFilteredQuery<EmployeeProjectDTO, EmployeeProjectFilterDTO>>().Object;
            var entityMapper = new EntityDTOMapper<EmployeeProject, EmployeeProjectDTO>();
            var respository = new EntityFrameworkRepository<EmployeeProject, int>(unitOfWorkProvider, dateTimeProvider);
            var parentRepository = new EntityFrameworkRepository<Employee, int>(unitOfWorkProvider, dateTimeProvider);
            var facade = new EmployeeProjectsFacade(queryFactory, entityMapper, respository, parentRepository, dateTimeProvider, unitOfWorkProvider);
            return facade;
        }

        private static void PrepareInMemoryDbContext(DbContextOptions<EmployeeProjectDbContext> options)
        {
            // Employee  Project  EmployeeProject
            //        1       10              100
            //        2       10              101
            //        2       11              102

            var employeeProject100 = new EmployeeProject
            {
                Id = 100,
                EmployeeId = 1,
                ProjectId = 10,
                ValidityBeginDate = DateTime.Today.AddDays(-1),
                ValidityEndDate = DateTime.Today.AddDays(1)
            };

            var employeeProject101 = new EmployeeProject
            {
                Id = 101,
                EmployeeId = 2,
                ProjectId = 10,
                ValidityBeginDate = DateTime.Today.AddDays(-1),
                ValidityEndDate = DateTime.Today.AddDays(1)
            };

            var employeeProject102 = new EmployeeProject
            {
                Id = 102,
                EmployeeId = 2,
                ProjectId = 11,
                ValidityBeginDate = DateTime.Today.AddDays(-1),
                ValidityEndDate = DateTime.Today.AddDays(1)
            };

            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = 1,
                    EmployeeProjects = new List<EmployeeProject> { employeeProject100 }
                },
                new Employee
                {
                    Id = 2,
                    EmployeeProjects = new List<EmployeeProject> { employeeProject101, employeeProject102 }
                }
            };

            var projects = new List<Project>
            {
                new Project
                {
                    Id = 10,
                    EmployeeProjects = new List<EmployeeProject> { employeeProject100, employeeProject101 }
                },
                new Project
                {
                    Id = 11,
                    EmployeeProjects = new List<EmployeeProject> { employeeProject102 }
                }
            };

            using (var ctx = new EmployeeProjectDbContext(options))
            {
                ctx.Employees.AddRange(employees);
                ctx.Projects.AddRange(projects);
                ctx.EmployeeProjects.Add(employeeProject100);
                ctx.EmployeeProjects.Add(employeeProject101);
                ctx.EmployeeProjects.Add(employeeProject102);
                ctx.SaveChanges();
            }
        }
    }
}
