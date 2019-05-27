﻿using Kosmograph.Messaging;
using System;

namespace Kosmograph.Model
{
    public class ModelController : IObserver<ChangedMessage<ITag>>, IObserver<ChangedMessage<IEntity>>, IObserver<ChangedMessage<IRelationship>>
    {
        private IDisposable tagSubscription;
        private IDisposable entitySubscription;
        private IDisposable relationshipSubscription;

        public ModelController(IKosmographMessageBus kosmographMessageBus)
            : this(kosmographMessageBus.Tags, kosmographMessageBus.Entities, kosmographMessageBus.Relationships)
        { }

        public ModelController(IChangedMessageBus<ITag> tags, IChangedMessageBus<IEntity> entities, IChangedMessageBus<IRelationship> relationships)
        {
            this.tagSubscription = tags.Subscribe(this);
            this.entitySubscription = entities.Subscribe(this);
            this.relationshipSubscription = relationships.Subscribe(this);
        }

        public Action<Tag> TagChanged { private get; set; }

        public Action<Guid> TagRemoved { private get; set; }

        public Action<Entity> EntityChanged { private get; set; }

        public Action<Guid> EntityRemoved { private get; set; }

        public Action<Relationship> RelationshipChanged { private get; set; }

        public Action<Guid> RelationshipRemoved { private get; set; }

        #region Observe Tags

        void IObserver<ChangedMessage<ITag>>.OnNext(ChangedMessage<ITag> value)
        {
            if (value.ChangeType.Equals(ChangeTypeValues.Modified))
                this.OnChangingTag((Tag)value.Changed);
            else
                this.OnRemovingTag(value.Changed.Id);
        }

        virtual protected void OnChangingTag(Tag tag) => this.TagChanged?.Invoke(tag);

        virtual protected void OnRemovingTag(Guid tagId) => this.TagRemoved?.Invoke(tagId);

        void IObserver<ChangedMessage<ITag>>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<ChangedMessage<ITag>>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        #endregion Observe Tags

        #region Observe Entities

        void IObserver<ChangedMessage<IEntity>>.OnNext(ChangedMessage<IEntity> value)
        {
            if (value.ChangeType.Equals(ChangeTypeValues.Modified))
                this.OnChangingEntity((Entity)value.Changed);
            else
                this.OnRemovingEntity(value.Changed.Id);
        }

        virtual protected void OnRemovingEntity(Guid entityId) => this.EntityRemoved?.Invoke(entityId);

        virtual protected void OnChangingEntity(Entity changed) => this.EntityChanged?.Invoke(changed);

        void IObserver<ChangedMessage<IEntity>>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<ChangedMessage<IEntity>>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        #endregion Observe Entities

        #region Observe Relationships

        void IObserver<ChangedMessage<IRelationship>>.OnNext(ChangedMessage<IRelationship> value)
        {
            if (value.ChangeType.Equals(ChangeTypeValues.Modified))
                this.OnChangingRelationship((Relationship)value.Changed);
            else
                OnRemovingRelationship(value.Changed.Id);
        }

        virtual protected void OnRemovingRelationship(Guid relationshipId) => this.RelationshipRemoved?.Invoke(relationshipId);

        virtual protected void OnChangingRelationship(Relationship changed) => this.RelationshipChanged?.Invoke(changed);

        void IObserver<ChangedMessage<IRelationship>>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<ChangedMessage<IRelationship>>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        #endregion Observe Relationships
    }
}