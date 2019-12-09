﻿using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using Kosmograph.Model;
using System.Collections.Generic;
using System.Linq;

namespace PSKosmograph.PathNodes
{
    public sealed class RootNode : IPathNode
    {
        private class Value : ContainerItemProvider
        {
            public Value()
               : base(new Item(), string.Empty)
            {
            }
        }

        public sealed class Item
        {
        }

        public object GetNodeChildrenParameters => null;

        public string Name => string.Empty;

        public string ItemMode => "+";

        public IEnumerable<IPathNode> GetNodeChildren(IProviderContext providerContext)
            => new IPathNode[] { new TagsNode(), new EntitiesNode(), new RelationshipsNode() };

        public IItemProvider GetItemProvider() => new Value();

        public IEnumerable<IPathNode> Resolve(IProviderContext providerContext, string? name)
        {
            if (name is null)
                return this.GetNodeChildren(providerContext);

            switch (name)
            {
                case "Tags":
                    return new TagsNode().Yield();

                case "Entities":
                    return new EntitiesNode().Yield();

                case "Relationships":
                    return new RelationshipsNode().Yield();
            }
            return Enumerable.Empty<IPathNode>();
        }
    }
}