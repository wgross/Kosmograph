﻿using Kosmograph.Desktop.ViewModel;
using Kosmograph.Model;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Kosmograph.Desktop.Test.ViewModel
{
    public class TagRepositoryViewModelTest : IDisposable
    {
        private readonly MockRepository mocks = new MockRepository(MockBehavior.Strict);
        private readonly Mock<ITagRepository> model;
        private TagRepositoryViewModel viewModel;

        public TagRepositoryViewModelTest()
        {
            this.model = this.mocks.Create<ITagRepository>();
            this.viewModel = new TagRepositoryViewModel(this.model.Object);
        }

        public void Dispose()
        {
            this.mocks.VerifyAll();
        }

        [Fact]
        public void TagRepositoryViewModel_mirrors_Model()
        {
            // ARRANGE

            var tag = new Tag("t");
            this.model
                .Setup(r => r.FindAll())
                .Returns(tag.Yield());

            // ACT

            this.viewModel.FillAll();

            // ASSERT

            Assert.Single(this.viewModel);
            Assert.Equal(tag, this.viewModel.Single().Model);
        }

        [Fact]
        public void TagRepositoryViewModel_delays_created_Tag_from_Model()
        {
            // ACT

            this.viewModel.CreateCommand.Execute(null);

            // ASSERT

            Assert.Empty(this.viewModel);
            Assert.Equal("new tag", this.viewModel.Edited.Name);
        }

        [Fact]
        public void TagRepositoryViewModel_commits_created_Tag_to_Model()
        {
            // ARRANGE

            var tag = new Tag("t");

            this.model
                .Setup(r => r.Upsert(It.IsAny<Tag>()))
                .Returns<Tag>(t => t);

            // create new tag
            this.viewModel.CreateCommand.Execute(null);

            // ACT

            this.viewModel.Edited.CommitCommand.Execute(null);

            // ASSERT
            // tag is inserted in the list after commit

            Assert.Equal("new tag", this.viewModel.Single().Name);
            Assert.Null(this.viewModel.Edited);
        }

        [Fact]
        public void TagRepositoryViewModel_reverts_created_Tag_at_Model()
        {
            // ARRANGE

            this.viewModel.CreateCommand.Execute(null);

            // ACT

            this.viewModel.Edited.RollbackCommand.Execute(null);

            // ASSERT
            // tag is forgotton

            Assert.Empty(this.viewModel);
            Assert.Null(this.viewModel.Edited);
        }

        [Fact]
        public void TagRepositoryViewModel_deletes_Tag_from_Model()
        {
            // ARRANGE

            var tag = new Tag("t");
            this.model
                .Setup(r => r.FindAll())
                .Returns(tag.Yield());

            this.model
                .Setup(r => r.Delete(tag.Id))
                .Returns(true);

            this.viewModel.FillAll();

            // ACT

            this.viewModel.DeleteCommand.Execute(this.viewModel.Single());

            // ASSERT

            Assert.Empty(this.viewModel);
        }
    }
}