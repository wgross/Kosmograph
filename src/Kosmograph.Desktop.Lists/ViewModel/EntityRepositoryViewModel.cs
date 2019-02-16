﻿using GalaSoft.MvvmLight.Command;
using Kosmograph.Messaging;
using Kosmograph.Model;
using System;
using System.Linq;

namespace Kosmograph.Desktop.Lists.ViewModel
{
    public class EntityRepositoryViewModel : RepositoryViewModelBase<EntityViewModel, IEntity>
    {
        private readonly IEntityRepository repository;

        public EntityRepositoryViewModel(IEntityRepository repository, IChangedMessageBus<IEntity> messaging)
            : base(messaging)
        {
            this.repository = repository;
            this.DeleteCommand = new RelayCommand<EntityViewModel>(this.DeleteCommandExecuted);
        }

        public void FillAll()
        {
            foreach (var vm in this.repository.FindAll().Select(t => new EntityViewModel(t)))
                this.Add(vm);
        }

        public RelayCommand<EntityViewModel> DeleteCommand { get; }

        private void DeleteCommandExecuted(EntityViewModel viewModel)
        {
            this.repository.Delete(viewModel.Model);
        }

        override protected void OnRemoved(Guid id)
        {
            var existingEntity = this.FirstOrDefault(e => e.Model.Id.Equals(id));
            if (existingEntity is null)
                return;
            this.Remove(existingEntity);
        }

        override protected void OnUpdated(Guid id)
        {
            try
            {
                var entity = new EntityViewModel(this.repository.FindById(id));
                var indexOfTag = this.IndexOf(entity);
                if (indexOfTag > -1)
                    this.SetItem(indexOfTag, entity);
                else
                    this.Add(entity);
            }
            catch (InvalidOperationException)
            {
                // throw on missing tag by Litedb -> consider map to KG exception type
                // remove item from list
                this.OnRemoved(id);
            }
        }
    }
}