﻿using Kosmograph.Desktop.ViewModel;
using Kosmograph.Model;
using System.Linq;
using Xunit;

namespace Kosmograph.Desktop.Test.ViewModel
{
    public class EditFacetPropertyViewModelTest
    {
        private readonly FacetProperty property;
        private readonly Facet facet;
        private readonly Tag tag;
        private readonly TagEditModel editTag;

        public EditFacetPropertyViewModelTest()
        {
            this.property = new FacetProperty("p1");
            this.facet = new Facet("f", this.property);
            this.tag = new Tag("tag", facet);
            this.editTag = new TagEditModel(tag, delegate { });
        }

        [Fact]
        public void EditFacetPropertyViewModelTest_mirrors_FacetProperty()
        {
            // ASSERT

            Assert.Single(editTag.Facet.Properties);
            Assert.Equal("f", editTag.Facet.Name);
        }

        [Fact]
        public void EditFacetPropertyViewModelTest_delays_changes_to_FacetProperty()
        {
            // ACT

            this.editTag.Facet.Properties.Single().Name = "p2";

            // ASSERT

            Assert.Equal("p1", this.property.Name);
            Assert.Equal("p2", this.editTag.Facet.Properties.Single().Name);
        }

        [Fact]
        public void EditFacetPropertyViewModel_commits_changes_to_FacetProperty()
        {
            // ARRANGE

            this.editTag.Facet.Properties.Single().Name = "p2";

            // ACT

            this.editTag.Commit();

            // ASSERT

            Assert.Equal("p2", this.property.Name);
            Assert.Equal("p2", this.editTag.Facet.Properties.Single().Name);
        }

        [Fact]
        public void EditFacetPropertyViewModel_reverts_changes_to_FacetProperty()
        {
            // ARRANGE

            this.editTag.Facet.Properties.Single().Name = "p2";

            // ACT

            this.editTag.Rollback();

            // ASSERT

            Assert.Equal("p1", this.property.Name);
            Assert.Equal("p1", this.editTag.Facet.Properties.Single().Name);
        }
    }
}