﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashing.IntegrationTests.SqlServer {
    using Dashing.IntegrationTests.SqlServer.Fixtures;
    using Dashing.IntegrationTests.TestDomain;

    using Xunit;

    public class FetchTests : IUseFixture<SqlServerFixture>
    {
        private SqlServerFixture fixture;

        [Fact]
        public void SimpleFetchWorks() {
            var posts = fixture.Session.Query<Post>().Fetch(p => p.Blog);
            Assert.NotNull(posts.First().Blog.Title);
        }

        [Fact]
        public void MultipleFetchParentWorks() {
            var posts = fixture.Session.Query<PostTag>().Fetch(p => p.Post).Fetch(p => p.Tag).ToList();
            Assert.NotNull(posts.First().Post.Title);
            Assert.NotNull(posts.First().Tag.Content);
        }

        [Fact]
        public void NestedFetchWorks() {
            var comment = fixture.Session.Query<Comment>().Fetch(c => c.Post.Blog);
            Assert.NotNull(comment.First().Post.Blog.Title);
        }

        [Fact]
        public void MultipleFetchWithNestedWorks() {
            var comment = fixture.Session.Query<Comment>().Fetch(c => c.Post.Blog).Fetch(c => c.User);
            Assert.NotNull(comment.First().Post.Blog.Title);
            Assert.NotNull(comment.First().User.Username);
        }

        [Fact]
        public void NullableFetchReturnsNull() {
            var comment = fixture.Session.Query<Comment>().Fetch(c => c.User).Where(c => c.Content == "Nullable User Content");
            Assert.Null(comment.First().User);
        }

        public void SetFixture(SqlServerFixture data) {
            this.fixture = data;
        }
    }
}