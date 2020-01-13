﻿using Kosmograph.LiteDb;
using Kosmograph.Messaging;
using Kosmograph.Model;
using Moq;
using System;
using System.Management.Automation;
using Xunit;

namespace PSKosmograph.Test
{
    [Collection("UsesPowershell")]
    public class KosmographCmdletProviderTestBase : IDisposable
    {
        public MockRepository Mocks { get; }

        public KosmographModel Model { get; }

        public Mock<IKosmographPersistence> PersistenceMock { get; }

        public Mock<ITagRepository> TagRepositoryMock { get; }

        public Mock<IEntityRepository> EntityRepositoryMock { get; }

        public Mock<ICategoryRepository> CategoryRepositoryMock { get; }

        public Mock<IRelationshipRepository> RelationshipRepositoryMock { get; }

        public PowerShell PowerShell { get; }

        public KosmographCmdletProviderTestBase()
        {
            this.Mocks = new MockRepository(MockBehavior.Strict);
            this.Model = this.CreateModel();
            this.PersistenceMock = this.Mocks.Create<IKosmographPersistence>();
            this.TagRepositoryMock = this.Mocks.Create<ITagRepository>();
            this.EntityRepositoryMock = this.Mocks.Create<IEntityRepository>();
            this.RelationshipRepositoryMock = this.Mocks.Create<IRelationshipRepository>();
            this.CategoryRepositoryMock = this.Mocks.Create<ICategoryRepository>();

            KosmographCmdletProvider.NewKosmographPersistence = _ => this.PersistenceMock.Object;

            this.PowerShell = PowerShell.Create();

            this.PowerShell
                .AddStatement()
                    .AddCommand("Import-Module")
                    .AddArgument("./PSKosmograph.dll")
                    .Invoke();

            this.PowerShell
                .AddStatement()
                    .AddCommand("New-PsDrive")
                        .AddParameter("Name", "kg")
                        .AddParameter("PsProvider", "Kosmograph")
                        .AddParameter("Root", @"kg:\")
                        .Invoke();

            this.PowerShell.Commands.Clear();
        }

        public void Dispose() => this.Mocks.VerifyAll();

        protected KosmographModel CreateModel()
        {
            var model = new KosmographModel(KosmographLiteDbPersistence.InMemory(KosmographMessageBus.Default));
            var tag1 = model.Tags.Upsert(new Tag("t1", new Facet("facet",
                new FacetProperty("text", FacetPropertyTypeValues.String),
                new FacetProperty("long", FacetPropertyTypeValues.Long))));
            var tag2 = model.Tags.Upsert(new Tag("t2", new Facet("facet", new FacetProperty("p2"))));
            var entity1 = model.Entities.Upsert(new Entity("e1", tag1));
            var entity2 = model.Entities.Upsert(new Entity("e2"));
            //var entity3 = model.Entities.Upsert(new Entity("entity3"));
            //var entity4 = model.Entities.Upsert(new Entity("entity4", tag1));
            var relationship1 = model.Relationships.Upsert(new Relationship("relationship1", entity1, entity2, tag2));
            // var relationship2 = model.Relationships.Upsert(new Relationship("relationship2", entity2, entity3, tag2));
            return model;
        }

        #region Arrangements

        protected void ArrangeEmptyRootCategory(out Category rootCategory)
        {
            rootCategory = DefaultCategory(AsRoot);
            this.PersistenceMock
                .Setup(p => p.Categories)
                .Returns(this.CategoryRepositoryMock.Object);

            this.CategoryRepositoryMock
                .Setup(r => r.Root())
                .Returns(rootCategory);
        }

        protected void ArrangeSubCategory(out Category rootCategory, out Category subCategory)
        {
            var subCategory_ = subCategory = DefaultCategory();
            var rootCategory_ = rootCategory = DefaultCategory(AsRoot);
            rootCategory.AddSubCategory(subCategory);

            this.PersistenceMock
                .Setup(p => p.Categories)
                .Returns(this.CategoryRepositoryMock.Object);

            this.CategoryRepositoryMock
                .Setup(r => r.Root())
                .Returns(rootCategory);

            // todo: resolve must use FindByCategeoryAndName instead of mathicng the names itself
            //this.CategoryRepositoryMock   
            //    .Setup(r => r.FindByCategoryAndName(rootCategory_, subCategory_.Name))
            //    .Returns(subCategory);
        }

        #endregion Arrangements

        #region Default Tag

        protected static Tag DefaultTag(params Action<Tag>[] setup)
        {
            var tmp = new Tag("t", new Facet("f"));
            setup.ForEach(s => s(tmp));
            return tmp;
        }

        protected static void WithDefaultProperty(Tag tag) => tag.Facet.AddProperty(new FacetProperty("p", FacetPropertyTypeValues.String));

        protected static void WithoutProperty(Tag tag) => tag.Facet.Properties.Clear();

        #endregion Default Tag

        #region Default Entity

        protected static Entity DefaultEntity(params Action<Entity>[] setup)
        {
            var tmp = new Entity("e");
            setup.ForEach(s => s(tmp));
            return tmp;
        }

        protected static void WithDefaultTag(Entity e) => e.AddTag(DefaultTag(WithDefaultProperty));

        protected static void WithoutTag(Entity e) => e.Tags.Clear();

        protected static Action<Entity> WithEntityCategory(Category c) => e => e.SetCategory(c);

        #endregion Default Entity

        #region Default Category

        protected static Category DefaultCategory(params Action<Category>[] setup)
        {
            var category = new Category("c");
            setup.ForEach(s => s(category));
            return category;
        }

        protected static void AsRoot(Category category)
        {
            category.Id = CategoryRepository.CategoryRootId;
            category.Name = "";
            category.Parent = null;
        }

        protected static Action<Category> WithSubCategory(Category category) => c => c.AddSubCategory(category);

        #endregion Default Category
    }
}