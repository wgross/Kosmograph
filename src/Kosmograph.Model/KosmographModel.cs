﻿namespace Kosmograph.Model
{
    public class KosmographModel
    {
        private IKosmographPersistence persistence;

        public KosmographModel(IKosmographPersistence persistence)
        {
            this.persistence = persistence;
        }

        public ITagRepository Tags => this.persistence.Tags;

        public IEntityRepository Entities => this.persistence.Entities;

        public IRelationshipRepository Relationships => this.persistence.Relationships;

        public Category RootCategory() => this.persistence.Categories.Root();
    }
}