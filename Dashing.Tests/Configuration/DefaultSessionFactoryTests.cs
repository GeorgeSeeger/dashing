﻿namespace Dashing.Tests.Configuration {
    using System.Data;

    using Dashing.Configuration;

    using Moq;

    using Xunit;

    public class DefaultSessionFactoryTests {
        private readonly Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();

        private readonly Mock<IDbConnection> mockConnection = new Mock<IDbConnection>();

        private readonly Mock<IDbTransaction> mockTransaction = new Mock<IDbTransaction>();

        [Fact]
        public void CreateReturnsASession() {
            var target = this.MakeTarget();

            var session = target.Create(this.mockConfig.Object, this.mockConnection.Object);

            Assert.NotNull(session);
            Assert.IsType<Session>(session);
        }

        [Fact]
        public void CreateWithTransactionReturnsASession() {
            var target = this.MakeTarget();

            var session = target.Create(this.mockConfig.Object, this.mockConnection.Object, this.mockTransaction.Object);

            Assert.NotNull(session);
            Assert.IsType<Session>(session);
            Assert.Same(this.mockTransaction.Object, session.Transaction);
        }

        private DefaultSessionFactory MakeTarget() {
            return new DefaultSessionFactory();
        }
    }
}