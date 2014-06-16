﻿namespace TopHat.Engine {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;

    using Dapper;

    using TopHat.Configuration;
    using TopHat.Extensions;

    internal class WhereClauseExpressionVisitor : BaseExpressionVisitor {
        private readonly ISqlDialect dialect;

        private readonly IConfiguration configuration;

        private readonly FetchNode rootNode;

        private bool isChainedMemberAccess;

        private Expression chainedMemberAccessExpression;

        private bool getForeignKeyName;

        private string chainedColumnName;

        private Type chainedColumnType;

        private readonly Stack<string> chainedEntityNames;

        private bool isClosureConstantAccess;

        private string constantMemberAccessName;

        private int paramCounter;

        private bool doAppendValue;

        private bool doPrependValue;

        private string appendValue = string.Empty;

        private string prependValue = string.Empty;

        private bool insideSubQuery;

        public StringBuilder Sql { get; private set; }

        public DynamicParameters Parameters { get; private set; }

        public WhereClauseExpressionVisitor(ISqlDialect dialect, IConfiguration config, FetchNode rootNode) {
            this.Sql = new StringBuilder();
            this.Parameters = new DynamicParameters();
            this.chainedEntityNames = new Stack<string>();
            this.dialect = dialect;
            this.configuration = config;
            this.rootNode = rootNode;
        }

        protected override Expression VisitBinary(BinaryExpression b) {
            // form binary expression
            this.Sql.Append("(");
            this.Visit(b.Left);
            this.Sql.Append(this.GetOperator(b.NodeType, b.Right.ToString() == "null"));
            this.Visit(b.Right);
            this.Sql.Append(")");

            return b;
        }

        protected override Expression VisitMemberAccess(MemberExpression m) {
            if (m.Expression.NodeType == ExpressionType.MemberAccess) {
                // in a chain of member access i.e. e.A.B.C == Z this is (e.A).B or (e.A.B).C
                if (this.isChainedMemberAccess) {
                    if (this.getForeignKeyName) {
                        // at this point let's fetch the foreign key name
                        this.chainedColumnName = this.configuration.GetMap(m.Member.DeclaringType).Columns[m.Member.Name].DbName;
                        this.chainedColumnType = m.Member.DeclaringType;
                        this.getForeignKeyName = false;
                    }
                    else {
                        this.chainedEntityNames.Push(m.Member.Name);
                    }
                }
                else {
                    this.isChainedMemberAccess = true;
                    this.chainedMemberAccessExpression = m;

                    if (this.configuration.HasMap(m.Member.DeclaringType)) {
                        // we want to check for a primary key here because in that case we can put the where clause on the referencing object
                        if (this.configuration.GetMap(m.Member.DeclaringType).PrimaryKey.Name == m.Member.Name) {
                            this.getForeignKeyName = true;
                        }
                        else {
                            // we need this column name
                            this.chainedColumnName = this.configuration.GetMap(m.Member.DeclaringType).Columns[m.Member.Name].DbName;
                            this.chainedColumnType = m.Member.DeclaringType;
                        }
                    }
                }
            }
            else if (m.Expression.NodeType == ExpressionType.Constant) {
                // we're getting a constant value out of an expression i.e. e.A == z.Prop we're doing z.Prop
                this.isClosureConstantAccess = true;
                this.constantMemberAccessName = m.Member.Name;
            }
            else if (m.Expression.NodeType == ExpressionType.Parameter) {
                // at the bottom of expression i.e. e.A.B.C == Z we're at e.A
                if (this.isChainedMemberAccess) {
                    if (this.getForeignKeyName) {
                        // we're at the bottom and we need to reference the foreign key column name
                        if (this.rootNode != null && this.rootNode.Alias.Length > 0) {
                            this.Sql.Append(this.rootNode.Alias + ".");
                        }

                        this.dialect.AppendQuotedName(this.Sql, this.configuration.GetMap(m.Member.DeclaringType).Columns[m.Member.Name].DbName);
                    }
                    else {
                        // we need to find the alias
                        // we've got chained entity names and the root node
                        this.chainedEntityNames.Push(m.Member.Name);
                        var currentNode = this.rootNode;
                        var numNames = this.chainedEntityNames.Count;
                        for (int i = 0; i < numNames; ++i) {
                            var propName = this.chainedEntityNames.Pop();
                            if (!currentNode.Children.ContainsKey(propName)) {
                                throw new Exception("You have to fetch a relationship in order to perform a where clause on it");
                            }

                            currentNode = currentNode.Children[propName];
                        }

                        if (!this.insideSubQuery) {
                            this.Sql.Append(currentNode.Alias + ".");
                        }

                        this.dialect.AppendQuotedName(this.Sql, this.configuration.GetMap(this.chainedColumnType).Columns[this.chainedColumnName].DbName);
                    }
                }
                else {
                    if (this.rootNode != null && this.rootNode.Alias.Length > 0) {
                        this.Sql.Append(this.rootNode.Alias + ".");
                    }

                    this.dialect.AppendQuotedName(this.Sql, this.configuration.GetMap(m.Member.DeclaringType).Columns[m.Member.Name].DbName);
                }
            }

            var expr = base.VisitMemberAccess(m);
            this.isChainedMemberAccess = false;
            this.isClosureConstantAccess = false;
            return expr;
        }

        protected override Expression VisitUnary(UnaryExpression u) {
            if (u.NodeType == ExpressionType.Not) {
                this.Sql.Append("not (");
            }

            base.VisitUnary(u);

            if (u.NodeType == ExpressionType.Not) {
                this.Sql.Append(")");
            }

            return u;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m) {
            Expression memberExpr = null;
            Expression valuesExpr = null;

            switch (m.Method.Name) {
                case "Contains":
                    if (m.Method.DeclaringType == typeof(string)) {
                        // this is string.Contains method
                        memberExpr = m.Object;
                        valuesExpr = m.Arguments[0];
                        this.Visit(memberExpr);
                        this.Sql.Append(" like ");
                        this.doPrependValue = this.doAppendValue = true;
                        this.prependValue = this.appendValue = "%";
                        this.Visit(valuesExpr);
                        this.doPrependValue = this.doAppendValue = false;
                        return m;
                    }

                    // this is IEnumerable.Contains method
                    if (m.Method.DeclaringType == typeof(Enumerable)) {
                        // static method
                        memberExpr = m.Arguments[1] as MemberExpression;
                        valuesExpr = m.Arguments[0];
                    }
                    else {
                        // contains on IList
                        memberExpr = m.Arguments[0] as MemberExpression;
                        valuesExpr = m.Object;
                    }

                    this.Visit(memberExpr);
                    this.Sql.Append(" in (");
                    this.Visit(valuesExpr);
                    this.Sql.Append(")");
                    break;

                case "StartsWith":
                    memberExpr = m.Object;
                    valuesExpr = m.Arguments[0];
                    this.Visit(memberExpr);
                    this.Sql.Append(" like ");
                    this.doAppendValue = true;
                    this.appendValue = "%";
                    this.Visit(valuesExpr);
                    this.doAppendValue = false;
                    break;

                case "EndsWith":
                    memberExpr = m.Object;
                    valuesExpr = m.Arguments[0];
                    this.Visit(memberExpr);
                    this.Sql.Append(" like ");
                    this.doPrependValue = true;
                    this.prependValue = "%";
                    this.Visit(valuesExpr);
                    this.doPrependValue = false;
                    break;

                case "Any":
                    memberExpr = m.Arguments[1];
                    var relatedType = m.Arguments[0].Type.GenericTypeArguments[0];
                    var map = this.configuration.GetMap(relatedType);
                    this.Sql.Append("exists (select 1 from ");
                    this.dialect.AppendQuotedTableName(this.Sql, map);
                    this.Sql.Append(" where ");
                    this.insideSubQuery = true;
                    this.Visit(m.Arguments[1]);
                    this.Sql.Append(")");
                    this.insideSubQuery = false;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return m;
        }

        protected override Expression VisitConstant(ConstantExpression c) {
            object value;
            if (this.isClosureConstantAccess) {
                value = Expression.Lambda(this.chainedMemberAccessExpression).Compile().DynamicInvoke(null);
            }
            else {
                value = c.Value;
            }

            if (value != null) {
                if (value.GetType().IsCollection()) {
                    throw new NotImplementedException();
                }

                this.Sql.Append("@l_" + ++this.paramCounter);
                this.Parameters.Add("@l_" + this.paramCounter, this.doAppendValue || this.doPrependValue ? this.WrapValue(value) : value);
            }

            return base.VisitConstant(c);
        }

        private object WrapValue(object value) {
            if (this.doPrependValue) {
                value = this.prependValue + value;
            }

            if (this.doAppendValue) {
                value = value + this.appendValue;
            }

            return value;
        }

        private string GetOperator(ExpressionType nodeType, bool isNull) {
            switch (nodeType) {
                case ExpressionType.Equal:
                    return isNull ? " is " : " = ";
                case ExpressionType.GreaterThanOrEqual:
                    return " >= ";
                case ExpressionType.GreaterThan:
                    return " > ";
                case ExpressionType.LessThanOrEqual:
                    return " <= ";
                case ExpressionType.LessThan:
                    return " < ";
                case ExpressionType.NotEqual:
                    return isNull ? " is not " : " != ";
                case ExpressionType.AndAlso:
                    return " and ";
                case ExpressionType.OrElse:
                    return " or ";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}