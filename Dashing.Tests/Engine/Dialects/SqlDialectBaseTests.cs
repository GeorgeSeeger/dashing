﻿namespace Dashing.Tests.Engine.Dialects {
    using System.Data;
    using System.Text;

    using Dashing.Configuration;
    using Dashing.Engine.Dialects;

    using Xunit;

    public class SqlDialectBaseTests {
        [Fact]
        public void AppendQuotedNameAppendsNameInQuoteCharacters() {
            var sb = new StringBuilder();
            var target = this.MakeTarget();

            target.AppendQuotedName(sb, "name");

            Assert.Equal("<name>", sb.ToString());
        }

        [Fact]
        public void AppendQuotedTableNameAppendsTableNameInQuoteCharacters() {
            var sb = new StringBuilder();
            var map = new Map<string> { Table = "foo" };
            var target = this.MakeTarget();

            target.AppendQuotedTableName(sb, map);

            Assert.Equal("<foo>", sb.ToString());
        }

        [Fact]
        public void AppendEscapedAppendsEscapedString() {
            var sb = new StringBuilder();
            var target = this.MakeTarget();

            target.AppendEscaped(sb, "O'Callahan");

            Assert.Equal("O''Callahan", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndTypeAndNotNull() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.Int32 };
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> int not null default 0", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndTypeAndNull() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.Int32, IsNullable = true }; // MJ should this throw an InvalidOperationException - seems nasty to have nullable db col with non nullable CLR type
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> int null", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndTypeAndPrimaryKey() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.Int32, IsPrimaryKey = true };
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> int not null primary key", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndIdentityAndPrimaryKey() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.Int32, IsPrimaryKey = true, IsAutoGenerated = true };
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> int not null generated always as identity primary key", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndLength() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.String, Length = 255 };
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> nvarchar(255) not null", sb.ToString());
        }

        [Fact]
        public void AppendColumnSpecificationAppendsNameAndMaxLength() {
            var sb = new StringBuilder();
            var col = new Column<int> { DbName = "foo", DbType = DbType.String, Length = 0 };
            var target = this.MakeTarget();

            target.AppendColumnSpecification(sb, col);

            Assert.Equal("<foo> nvarchar(max) not null", sb.ToString());
        }

        private TestDialect MakeTarget() {
            return new TestDialect();
        }

        private class TestDialect : SqlDialectBase {
            public TestDialect()
                : base('<', '>') {
            }

            public override string ChangeColumnName(IColumn fromColumn, IColumn toColumn) {
                throw new System.NotImplementedException();
            }

            public override string ModifyColumn(IColumn fromColumn, IColumn toColumn) {
                throw new System.NotImplementedException();
            }

            public override string DropForeignKey(ForeignKey foreignKey) {
                throw new System.NotImplementedException();
            }

            public override string DropIndex(Index index) {
                throw new System.NotImplementedException();
            }

            public override void ApplySkipTake(StringBuilder sql, StringBuilder orderClause, int take, int skip) {
                throw new System.NotImplementedException();
            }
        }
    }
}