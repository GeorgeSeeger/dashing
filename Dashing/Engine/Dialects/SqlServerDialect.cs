namespace Dashing.Engine.Dialects {
    using System;
    using System.Data;
    using System.Linq;
    using System.Text;

    using Dashing.Configuration;

    public class SqlServerDialect : SqlDialectBase {
        private const char DotCharacter = '.';

        public SqlServerDialect()
            : base('[', ']') {
        }

        public override void AppendQuotedTableName(StringBuilder sql, IMap map) {
            if (map.Schema != null) {
                this.AppendQuotedName(sql, map.Schema);
                sql.Append(DotCharacter);
            }

            this.AppendQuotedName(sql, map.Table);
        }

        protected override void AppendAutoGenerateModifier(StringBuilder sql, IColumn column) {
            if (column.Type == typeof(Guid)) {
                sql.Append(" DEFAULT NEWSEQUENTIALID()");
            }
            else {
                sql.Append(" identity(1,1)");
            }
        }

        protected override string TypeName(DbType type) {
            switch (type) {
                case DbType.Boolean:
                    return "bit";

                case DbType.DateTime2:
                    return "datetime2";

                case DbType.Guid:
                    return "uniqueidentifier";

                case DbType.Object:
                    return "sql_variant";

                default:
                    return base.TypeName(type);
            }
        }

        public override string ChangeColumnName(IColumn fromColumn, IColumn toColumn) {
            return "EXEC sp_RENAME '" + toColumn.Map.Table + "." + fromColumn.DbName + "', '" + toColumn.DbName + "', 'COLUMN'";
        }

        public override string ModifyColumn(IColumn fromColumn, IColumn toColumn) {
            var sql = new StringBuilder();

            // drop a column constraint if need be
            if (!string.IsNullOrEmpty(fromColumn.Default) && !fromColumn.IsPrimaryKey && !fromColumn.IsAutoGenerated
                && (string.IsNullOrEmpty(toColumn.Default) || fromColumn.Default != toColumn.Default)) {
                sql.AppendLine(this.OnBeforeDropColumn(fromColumn));
            }

            // alter the column
            sql.Append("alter table ");
            this.AppendQuotedTableName(sql, toColumn.Map);
            sql.Append(" alter column ");
            this.AppendColumnSpecification(sql, toColumn, false);

            // add a column constraint if need be
            if (!string.IsNullOrEmpty(toColumn.Default) && !toColumn.IsPrimaryKey && !toColumn.IsAutoGenerated) {
                sql.AppendLine(";");
                sql.Append("alter table ");
                this.AppendQuotedTableName(sql, toColumn.Map);
                sql.Append(" add default (");
                sql.Append(toColumn.Default);
                sql.Append(") for");
                this.AppendQuotedName(sql, toColumn.DbName);
            }

            return sql.ToString();
        }

        public override string DropForeignKey(ForeignKey foreignKey) {
            var sql = new StringBuilder("alter table ");
            this.AppendQuotedTableName(sql, foreignKey.ChildColumn.Map);
            sql.Append(" drop constraint ");
            this.AppendQuotedName(sql, foreignKey.Name);
            return sql.ToString();
        }

        public override string DropIndex(Index index) {
            var sql = new StringBuilder("drop index ");
            this.AppendQuotedName(sql, index.Name);
            sql.Append(" on ");
            this.AppendQuotedTableName(sql, index.Map);
            return sql.ToString();
        }

        public override void ApplySkipTake(StringBuilder sql, StringBuilder orderClause, int take, int skip) {
            if (skip == 0) {
                // query starts with SELECT so insert top (X) there
                sql.Insert(6, " top (@take)");
                return;
            }

            // now we have take and skip - we'll do the recursive CTE thingy
            sql.Insert(6, " ROW_NUMBER() OVER (" + orderClause + ") as RowNum,");
            sql.Insert(0, "select * from (");

            // see MySqlDialect for explanation of the crazy number 18446744073709551615
            sql.Append(
                ") as pagetable where pagetable.RowNum between @skip + 1 and " + (take > 0 ? "@skip + @take" : "18446744073709551615")
                + " order by pagetable.RowNum");
        }

        public override string CreateIndex(Index index) {
            var statement = base.CreateIndex(index);
            if (index.IsUnique && index.Columns.Any(c => c.IsNullable)) {
                var whereClause = new StringBuilder();
                whereClause.Append(" where ");
                bool first = true;
                foreach (var column in index.Columns.Where(c => c.IsNullable)) {
                    if (!first) {
                        whereClause.Append(" and ");
                    }

                    this.AppendQuotedName(whereClause, column.DbName);
                    whereClause.Append(" is not null");
                    first = false;
                }
                statement += whereClause.ToString();
            }

            return statement;
        }

        public override string ChangeTableName(IMap @from, IMap to) {
            var sql = new StringBuilder("EXEC sp_RENAME ");
            this.AppendQuotedTableName(sql, from);
            sql.Append(", ");
            this.AppendQuotedTableName(sql, to);
            return sql.ToString();
        }

        public override string CheckDatabaseExists(string databaseName) {
            return "SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '" + databaseName + "' OR name = '" + databaseName + "')";
        }

        public override string GetIdSql() {
            return string.Empty;
        }

        public override void AppendIdOutput(StringBuilder sql, IMap map) {
            sql.Append(" output inserted.")
               .Append(this.BeginQuoteCharacter)
               .Append(map.PrimaryKey.DbName)
               .Append(this.EndQuoteCharacter);
        }

        public override void AppendForUpdateUsingTableHint(StringBuilder tableSql) {
            tableSql.Append(" with (rowlock, xlock)");
        }

        public override void AppendForUpdateOnQueryFinish(StringBuilder sql) {
        }

        public override string OnBeforeDropColumn(IColumn column) {
            var commandName = "@OBDCommand" + Guid.NewGuid().ToString("N");
            var sb =
                new StringBuilder("declare ").Append(commandName)
                                             .AppendLine(" nvarchar(1000);")
                                             .Append("select ")
                                             .Append(commandName)
                                             .Append(" = 'ALTER TABLE ");
            this.AppendQuotedTableName(sb, column.Map);
            sb.Append(" drop constraint ' + d.name ").Append(@"from sys.tables t   
                          join    sys.default_constraints d       
                           on d.parent_object_id = t.object_id  
                          join    sys.columns c      
                           on c.object_id = t.object_id      
                            and c.column_id = d.parent_column_id
                         where t.name = '");
            sb.Append(column.Map.Table).Append("' and c.name = '").Append(column.DbName).AppendLine("';");
            sb.Append("execute(").Append(commandName).AppendLine(");");
            return sb.ToString();
        }
    }
}