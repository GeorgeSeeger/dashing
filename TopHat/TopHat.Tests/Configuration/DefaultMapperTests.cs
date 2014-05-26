﻿namespace TopHat.Tests.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq.Expressions;

    using Moq;

    using global::TopHat.Configuration;

    using global::TopHat.Tests.TestDomain;

    using Xunit;

    public class DefaultMapperTests {
        private readonly Mock<IConvention> mockConvention = new Mock<IConvention>();

        private const string ExampleString = "foo";

        private const string Username = "Username";

        private const byte ExampleByte = 128;

        private const ushort ExampleUshort = 1024;

        [Fact]
        public void ConstructorThrowsOnNullConvention() {
            Assert.Throws<ArgumentNullException>(() => new DefaultMapper(null));
        }

        [Fact]
        public void DelegatesTableNameToConvention() {
            this.mockConvention.Setup(m => m.TableFor(typeof(User))).Returns(ExampleString).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            Assert.Equal(ExampleString, map.Table);
            this.mockConvention.Verify();
        }

        [Fact]
        public void DelegatesSchemaNameToConvention() {
            this.mockConvention.Setup(m => m.SchemaFor(typeof(User))).Returns(ExampleString).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            Assert.Equal(ExampleString, map.Schema);
            this.mockConvention.Verify();
        }

        [Fact]
        public void DelegatesPrimaryKeyNameToConvention() {
            this.mockConvention.Setup(m => m.PrimaryKeyFor(typeof(User))).Returns(Username).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            Assert.NotNull(map.PrimaryKey);
            Assert.Equal(Username, map.PrimaryKey.Name);
            this.mockConvention.Verify();
        }

        [Fact]
        public void StringLengthDelegatesToConvention() {
            this.mockConvention.Setup(m => m.StringLengthFor(typeof(User), "Username")).Returns(ExampleUshort).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            var property = map.Property(u => u.Username);
            Assert.NotNull(property);
            Assert.Equal(ExampleUshort, property.Length);
            this.mockConvention.Verify();
        }

        [Fact]
        public void DecimalPrecisionDelegatesToConvention() {
            this.mockConvention.Setup(m => m.DecimalPrecisionFor(typeof(User), "HeightInMeters")).Returns(ExampleByte).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            var property = map.Property(u => u.HeightInMeters);
            Assert.NotNull(property);
            Assert.Equal(ExampleByte, property.Precision);
            this.mockConvention.Verify();
        }

        [Fact]
        public void DecimalScaleDelegatesToConvention() {
            this.mockConvention.Setup(m => m.DecimalScaleFor(typeof(User), "HeightInMeters")).Returns(ExampleByte).Verifiable();
            var target = this.MakeTarget();

            var map = target.MapFor<User>();

            Assert.NotNull(map);
            var property = map.Property(u => u.HeightInMeters);
            Assert.NotNull(property);
            Assert.Equal(ExampleByte, property.Scale);
            this.mockConvention.Verify();
        }

        [Fact]
        public void ColumnTypeIsSet() {
            var property = this.MapAndGetProperty<Post, string>(u => u.Content);
            Assert.Equal(typeof(string), property.Type);
        }

        [Fact]
        public void ColumnNameIsSet() {
            var property = this.MapAndGetProperty<Post, string>(u => u.Content);
            Assert.Equal("Content", property.Name);
        }

        [Fact]
        public void NonEntityColumnDbTypeIsSet() {
            var property = this.MapAndGetProperty<Post, string>(u => u.Content);
            Assert.Equal(DbType.String, property.DbType);
        }

        [Fact]
        public void NonEntityColumnDbNameIsSet() {
            var property = this.MapAndGetProperty<Post, string>(u => u.Content);
            Assert.Equal("Content", property.DbName);
        }

        [Fact]
        public void NonEntityColumnRelationshipIsNone() {
            var property = this.MapAndGetProperty<Post, string>(u => u.Content);
            Assert.Equal(RelationshipType.None, property.Relationship);
        }

        [Fact]
        public void EntityColumnDbTypeIsSet() {
            var property = this.MapAndGetProperty<Post, User>(p => p.Author);
            Assert.Equal(DbType.Int32, property.DbType);
        }

        [Fact]
        public void EntityColumnDbNameIsSet() {
            var property = this.MapAndGetProperty<Post, User>(p => p.Author);
            Assert.Equal("AuthorId", property.DbName);
        }

        [Fact]
        public void EntityColumnRelationshipIsManyToOne() {
            var property = this.MapAndGetProperty<Post, User>(u => u.Author);
            Assert.Equal(RelationshipType.ManyToOne, property.Relationship);
        }

        [Fact(Skip = "Check with Mark what he expects here")]
        public void EntityCollectionIsIgnored() {
            var property = this.MapAndGetProperty<Post, IList<Comment>>(p => p.Comments);
            Assert.Equal(true, property.IsIgnored);
        }

        [Fact]
        public void EntityCollectionColumnRelationshipIsOneToMany() {
            var property = this.MapAndGetProperty<Post, IList<Comment>>(p => p.Comments);
            Assert.Equal(RelationshipType.OneToMany, property.Relationship);
        }

        private DefaultMapper MakeTarget() {
            return new DefaultMapper(this.mockConvention.Object);
        }

        private Column<TProperty> MapAndGetProperty<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression) {
            var target = this.MakeTarget();
            var map = target.MapFor<T>();
            Assert.NotNull(map);
            var property = map.Property(propertyExpression);
            Assert.NotNull(property);
            return property;
        }
    }
}