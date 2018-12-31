﻿using Kosmograph.Desktop.Lists.ViewModel;
using Kosmograph.Messaging;
using Kosmograph.Model;
using Moq;
using System;
using System.Linq;
using Xunit;

namespace Kosmograph.Desktop.Lists.Test.ViewModel
{
    public class EntityRepositoryViewModelTest : IDisposable
    {
        private readonly EntityRepositoryViewModel repositoryViewModel;
        private readonly EntityMessageBus messaging = new EntityMessageBus();
        private readonly MockRepository mocks = new MockRepository(MockBehavior.Strict);
        private readonly Mock<IEntityRepository> repository;

        public EntityRepositoryViewModelTest()
        {
            this.repository = this.mocks.Create<IEntityRepository>();
            this.repositoryViewModel = new EntityRepositoryViewModel(this.repository.Object, this.messaging);
        }

        public void Dispose()
        {
            this.mocks.VerifyAll();
        }

        public Tag DefaultTag() => new Tag("tag", new Facet("facet", new FacetProperty("p")));

        public Entity DefaultEntity() => new Entity("entity", DefaultTag());

        [Fact]
        public void EntityRepositoryViewModel_creates_TagViewModel_from_all_Tags()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindAll())
                .Returns(entity.Yield());

            // ACT

            this.repositoryViewModel.FillAll();

            // ASSERT
            // repos contains the single tag

            Assert.Equal(entity, this.repositoryViewModel.Single().Model);
        }

        [Fact]
        public void EntityRepositoryViewModel_replaces_EntityViewModel_on_updated()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindAll())
                .Returns(entity.Yield());

            this.repository
                .Setup(r => r.FindById(entity.Id))
                .Returns(entity);

            this.repositoryViewModel.FillAll();
            var originalViewModel = this.repositoryViewModel.Single();

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                Assert.Same(this.repositoryViewModel, sender);
                Assert.Equal(entity, e.NewItems.OfType<EntityViewModel>().Single().Model);
                Assert.Equal(entity, e.OldItems.OfType<EntityViewModel>().Single().Model);
                collectionChanged = true;
            };

            // ACT

            this.messaging.Modified(entity);

            // ASSERT

            Assert.True(collectionChanged);
            Assert.NotSame(originalViewModel, this.repositoryViewModel.Single());
            Assert.Equal(entity, this.repositoryViewModel.Single().Model);
        }

        [Fact]
        public void EntityRepositoryViewModel_replacing_EntityViewModel_on_updated_removes_missing_Tag()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindAll())
                .Returns(entity.Yield());

            this.repository
                .Setup(r => r.FindById(entity.Id))
                .Throws<InvalidOperationException>();

            this.repositoryViewModel.FillAll();
            var originalViewModel = this.repositoryViewModel.Single();

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                Assert.Same(this.repositoryViewModel, sender);
                Assert.Null(e.NewItems);
                Assert.Equal(entity, e.OldItems.OfType<EntityViewModel>().Single().Model);
                collectionChanged = true;
            };

            // ACT

            this.messaging.Modified(entity);

            // ASSERT
            
            Assert.True(collectionChanged);
            Assert.False(this.repositoryViewModel.Any());
        }

        [Fact]
        public void EntityRepositoryViewModel_removes_EntityViewModel_on_removed()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindAll())
                .Returns(entity.Yield());

            this.repositoryViewModel.FillAll();
            var originalViewModel = this.repositoryViewModel.Single();

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                Assert.Same(this.repositoryViewModel, sender);
                Assert.Null(e.NewItems);
                Assert.Equal(entity, e.OldItems.OfType<EntityViewModel>().Single().Model);
                collectionChanged = true;
            };

            // ACT

            this.messaging.Removed(entity);

            // ASSERT

            Assert.True(collectionChanged);
            Assert.False(this.repositoryViewModel.Any());
        }

        [Fact]
        public void EntityRepositoryViewModel_adds_EntityViewModel_on_updated()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindById(entity.Id))
                .Returns(entity);

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                Assert.Same(this.repositoryViewModel, sender);
                Assert.Equal(entity, e.NewItems.OfType<EntityViewModel>().Single().Model);
                Assert.Null(e.OldItems);
                collectionChanged = true;
            };

            // ACT

            this.messaging.Modified(entity);

            // ASSERT
            // same tag, but new view model instance

            Assert.True(collectionChanged);
            Assert.Equal(entity, this.repositoryViewModel.Single().Model);
        }

        [Fact]
        public void EntityRepositoryViewModel_adding_EntityViewModel_on_updated_ignores_missing_entty()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindById(entity.Id))
                .Throws<InvalidOperationException>();

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                collectionChanged = true;
            };

            // ACT

            this.messaging.Modified(entity);

            // ASSERT

            Assert.False(collectionChanged);
            Assert.False(this.repositoryViewModel.Any());
        }

        [Fact]
        public void EntityRepositoryViewModel_removes_Entity()
        {
            // ARRANGE

            var entity = DefaultEntity();

            this.repository
                .Setup(r => r.FindAll())
                .Returns(entity.Yield());

            // deleting a tag at the repos raises a changed event
            this.repository
                .Setup(r => r.Delete(entity))
                .Callback(() => this.messaging.Removed(entity))
                .Returns(true);

            this.repositoryViewModel.FillAll();
            var originalViewModel = this.repositoryViewModel.Single();

            bool collectionChanged = false;
            this.repositoryViewModel.CollectionChanged += (sender, e) =>
            {
                Assert.Same(this.repositoryViewModel, sender);
                Assert.Null(e.NewItems);
                Assert.Equal(entity, e.OldItems.OfType<EntityViewModel>().Single().Model);
                collectionChanged = true;
            };

            // ACT

            this.repositoryViewModel.DeleteCommand.Execute(this.repositoryViewModel.Single());

            // ASSERT
            // same tag, but new view model instance

            Assert.True(collectionChanged);
            Assert.False(this.repositoryViewModel.Any());
        }
    }
}