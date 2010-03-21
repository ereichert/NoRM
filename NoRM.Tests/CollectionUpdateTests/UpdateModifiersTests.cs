using System;
using Xunit;
using Norm.Collections;

namespace Norm.Tests.CollectionUpdateTests
{
    public class UpdateModifiersTests : IDisposable
    {

        private readonly Mongo _server;
        private readonly MongoCollection<Post> _collection;

        public UpdateModifiersTests()
        {
            _server = Mongo.ParseConnection("mongodb://localhost/NormTests?pooling=false&strict=true");
            _collection = _server.GetCollection<Post>("Posts");
        }
        public void Dispose()
        {
            _server.Database.DropCollection("Posts");
            using (var admin = new MongoAdmin("mongodb://localhost/NormTests?pooling=false"))
            {
                admin.DropDatabase();
            }
            _server.Dispose();
        }


        [Fact]
        public void PostScoreShouldBeEqualThreeWhenApplyingIncrementBy2CommandToScoreEqOne()
        {
            var post = new Post { Title = "About the name", Score = 1 };
            _collection.Insert(post);
            
            _collection.UpdateOne(new { _id = post.Id }, new { Score = M.Inc(2), Title = M.Set("ss") });

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(3, result.Score);
        }

        [Fact]
        public void PostScoreShouldBeEqualOneWhenApplyingIncrementByMinus2CommandToScoreEqThree()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new { Score = M.Inc(-2) });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Score);
        }
        [Fact]
        public void PostTitleShouldBeEqual_NoRm_WhenApplyingSetModifierCommandToTitle()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new { Title = M.Set("NoRm") });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal("NoRm", result.Title);
            Assert.Equal(3, result.Score);
        }
        [Fact]
        public void PostCommentsCountShouldBeEqualOneWhenApplyingPushModifierCommandToPostWithNoComments()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new { Comments = M.Push(new Comment { Text = "SomeText" }) });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Comments.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }
        [Fact]
        public void PostCommentsCountShouldBeEqualTwoWhenApplyingPushModifierCommandToPostWithOneComment()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Comments.Add(new Comment { Text = "some text" });
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new { Comments = M.Push(new Comment { Text = "SomeText" }) });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(2, result.Comments.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PostCommentsCountShouldBeEqualTwoWhenApplyingPushAllModifieWith2CommentsToPostWithNoComments()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };

            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new
            {
                Comments = M.PushAll(
                    new Comment { Text = "SomeText" },
                    new Comment { Text = "SecondComment" })
            });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(2, result.Comments.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PostCommentsCountShouldBeEqualThreeWhenApplyingPushAllModifieWith2CommentsToPostWithOneComment()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Comments.Add(new Comment { Text = "some text" });
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new
            {
                Comments = M.PushAll(
                    new Comment { Text = "SomeText" },
                    new Comment { Text = "SecondComment" })
            });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(3, result.Comments.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }


        [Fact(Skip = "AddToSet works only with mongoDB 1.3.3 +")]
        public void AddingTag_NoSql_ToPostWithoutTagsWithAddToSetModifierShouldAddThatTag()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new
            {
                Tags = M.AddToSet("NoSql")
            });
            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Tags.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }
        [Fact(Skip = "AddToSet works only with mongoDB 1.3.3 +")]
        public void AddingTag_NoSql_ToPostWith_NoSql_TagWithAddToSetModifierShouldNotAddThatTag()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            _collection.Insert(post);

            _collection.UpdateOne(new { _id = post.Id }, new
            {
                Tags = M.AddToSet("NoSql")
            });

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Tags.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PullingTag_NoSql_FromPostWith_NoSql_TagWithPullModifierShouldRemoveThatTag()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            _collection.Insert(post);

            _collection.UpdateWithModifier(post.Id, op => op.Pull(prop => prop.Tags, "NoSql"));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(0, result.Tags.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PullingTag_NoSql_FromPostWithout_NoSql_TagWithPullModifierShouldDoNothing()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql2");
            _collection.Insert(post);
            _collection.UpdateWithModifier(post.Id, op => op.Pull(prop => prop.Tags, "NoSql"));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Tags.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PullingTag_NoSql_FromPostWith_NoSql_Tag_And_ABC_TagWithPullModifierShouldRemoveOnly_NoSql_Tag()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            post.Tags.Add("ABC");
            _collection.Insert(post);
            _collection.UpdateWithModifier(post.Id, op => op.Pull(prop => prop.Tags, "NoSql"));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Tags.Count);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);
        }

        [Fact]
        public void PopModifierLastItemUsage()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            post.Tags.Add("ABC");
            post.Tags.Add("mongo");
            _collection.Insert(post);
            _collection.UpdateWithModifier(post.Id, op => op.PopLast(prop => prop.Tags));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(2, result.Tags.Count);
            Assert.DoesNotContain("mongo",result.Tags);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);

        }
        [Fact]
        public void PopModifierFirstItemUsage()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            post.Tags.Add("ABC");
            post.Tags.Add("mongo");
            _collection.Insert(post);
            _collection.UpdateWithModifier(post.Id, op => op.PopFirst(prop => prop.Tags));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(2, result.Tags.Count);
            Assert.DoesNotContain("NoSql", result.Tags);
            Assert.Equal(3, result.Score);
            Assert.Equal("About the name 2", result.Title);

        }

        [Fact]
        public void PullAllModifierUsage()
        {
            var post = new Post { Title = "About the name 2", Score = 3 };
            post.Tags.Add("NoSql");
            post.Tags.Add("ABC");
            post.Tags.Add("mongo");
            _collection.Insert(post);
            _collection.UpdateWithModifier(post.Id, op => op.PullAll(prop => prop.Tags,"NoSql","ABC"));

            var result = _collection.FindOne(new { _id = post.Id });
            Assert.Equal(1, result.Tags.Count);
            Assert.DoesNotContain("NoSql", result.Tags);
            Assert.DoesNotContain("ABC", result.Tags);


        }



    }
}