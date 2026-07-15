using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Moq;

namespace InduccionRefactorization.Tests.TestHelpers
{
    /// <summary>
    /// Helper class to create mockable DbSet instances for Entity Framework testing
    /// </summary>
    public static class MockDbSetHelper
    {
        /// <summary>
        /// Creates a mock DbSet from a list of entities
        /// </summary>
        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            // Setup IQueryable methods
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            // Setup Add method
            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);

            // Setup Remove method
            mockSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(entity => data.Remove(entity));

            // Setup Find method
            mockSet.Setup(m => m.Find(It.IsAny<object[]>()))
                .Returns<object[]>(ids => data.FirstOrDefault());

            return mockSet;
        }

        /// <summary>
        /// Creates a mock DbSet with Include support for navigation properties
        /// </summary>
        public static Mock<DbSet<T>> CreateMockDbSetWithIncludes<T>(List<T> data) where T : class
        {
            var mockSet = CreateMockDbSet(data);

            // Setup Include method to return the same mock set (for fluent API)
            mockSet.Setup(m => m.Include(It.IsAny<string>())).Returns(mockSet.Object);

            return mockSet;
        }
    }
}
