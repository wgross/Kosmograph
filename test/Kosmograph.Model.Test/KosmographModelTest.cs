﻿using Moq;
using Xunit;

namespace Kosmograph.Model.Test
{
    public class KosmographModelTest : ModelTestBase
    {
        private readonly Mock<ICategoryRepository> categoryRepository;
        private readonly Mock<ITagRepository> tagRepository;
        private readonly KosmographModel model;

        public KosmographModelTest()
        {
            this.categoryRepository = this.Mocks.Create<ICategoryRepository>();
            this.tagRepository = this.Mocks.Create<ITagRepository>();
            this.model = this.NewModel();
            // just touch the message bus to satify the strict mock repo
            var msgBus = this.model.MessageBus;
        }

        [Fact]
        public void KosmographModel_provides_root_Category()
        {
            // ARRANGE

            this.Persistence
                .Setup(p => p.Categories)
                .Returns(this.categoryRepository.Object);

            this.categoryRepository
                .Setup(p => p.Root())
                .Returns(new Category());

            // ACT

            var result = this.model.RootCategory();

            // ASSERT

            Assert.NotNull(result);
        }

        [Fact]
        public void KosmographModel_provides_tags()
        {
            // ARRANGE

            this.Persistence
                .Setup(p => p.Tags)
                .Returns(this.tagRepository.Object);

            // ACT

            var result = this.model.Tags;

            // ASSERT

            Assert.Same(result, this.tagRepository.Object);
        }

        [Fact]
        public void KosmographModel_disposes_persistence_on_model_disposing()
        {
            // ARRANGE

            this.Persistence
                .Setup(p => p.Dispose());

            // ACT

            this.model.Dispose();
        }
    }
}