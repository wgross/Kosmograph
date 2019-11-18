﻿using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Kosmograph.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PSKosmograph.PathNodes
{
    public class EntityNode : IPathNode, INewItem, IRemoveItem, ICopyItem
    {
        public sealed class Value : ContainerPathValue, IPathValue
        {
            private readonly Entity entity;
            private readonly IKosmographPersistence model;

            public Value(IKosmographPersistence model, Entity entity)
                : base(new Item(entity), entity.Name)
            {
                this.entity = entity;
                this.model = model;
            }

            public void SetItemProperties(IEnumerable<PSPropertyInfo> properties)
            {
                IPathValue.SetItemProperties(this, properties);
                this.model.Entities.Upsert(entity);
            }
        }

        public sealed class Item
        {
            private readonly Entity entitiy;

            public Item(Entity entity)
            {
                this.entitiy = entity;
            }

            public string Name
            {
                get => this.entitiy.Name;
                set => this.entitiy.Name = value;
            }

            public Guid Id => this.entitiy.Id;
        }

        private readonly Entity entity;
        private readonly IKosmographPersistence model;

        public EntityNode(IKosmographPersistence model, Entity entity)
        {
            this.entity = entity;
            this.model = model;
        }

        #region IPathNode Members

        public string Name => this.entity.Name;

        public string ItemMode => "+";

        public IEnumerable<IPathNode> GetNodeChildren(IProviderContext providerContext)
            => this.entity.Tags.Select(t => new AssignedTagNode(providerContext.Persistence(), this.entity, t));

        public IPathValue GetNodeValue() => new Value(this.model, this.entity);

        public IEnumerable<IPathNode> Resolve(IProviderContext providerContext, string? name)
        {
            if (name is null)
                return this.GetNodeChildren(providerContext);

            var tag = this.entity.Tags.SingleOrDefault(t => t.Name.Equals(name));
            if (tag is null)
                return Enumerable.Empty<IPathNode>();
            return new AssignedTagNode(providerContext.Persistence(), this.entity, tag).Yield();
        }

        #endregion IPathNode Members

        #region INewItem Members

        public IEnumerable<string> NewItemTypeNames => "AssignedTag".Yield();

        public IPathValue NewItem(IProviderContext providerContext, string newItemChildPath, string? itemTypeName, object? newItemValue)
        {
            var tag = providerContext.Persistence().Tags.FindByName(newItemChildPath);
            if (tag is null)
                throw new InvalidOperationException($"tag(name='{newItemChildPath}') doesn't exist.");

            if (this.entity.Tags.Contains(tag))
                return new AssignedTagNode(providerContext.Persistence(), this.entity, tag).GetNodeValue();

            this.entity.AddTag(tag);

            providerContext.Persistence().Entities.Upsert(this.entity);

            return new AssignedTagNode(providerContext.Persistence(), this.entity, tag).GetNodeValue();
        }

        #endregion INewItem Members

        #region IRemoveItem Members

        public void RemoveItem(IProviderContext providerContext, string path, bool recurse)
        {
            if (recurse)
                providerContext.Persistence().Entities.Delete(this.entity);
            else if (!this.entity.Tags.Any())
                providerContext.Persistence().Entities.Delete(this.entity);
        }

        #endregion IRemoveItem Members

        #region ICopyItem Members

        public void CopyItem(IProviderContext providerContext, string sourceItemName, string destinationItemName, IPathValue destinationContainer, bool recurse)
        {
            var newEntity = new Entity(destinationItemName, this.entity.Tags.ToArray());

            this.entity.Tags.ForEach(t =>
            {
                t.Facet.Properties.ForEach(p =>
                {
                    (var hasValue, var value) = this.entity.TryGetFacetProperty(p);
                    if (hasValue)
                        newEntity.SetFacetProperty(p, value);
                });
            });
            providerContext.Persistence().Entities.Upsert(newEntity);
        }

        #endregion ICopyItem Members
    }
}