﻿using GalaSoft.MvvmLight.Command;
using Kosmograph.Desktop.Editors.ViewModel.Base;
using Kosmograph.Model;
using System.Linq;
using System.Windows.Input;

namespace Kosmograph.Desktop.Editors.ViewModel
{
    public class TagEditModel : NamedEditModelBase<Tag>
    {
        private readonly ITagEditCallback editCallback;

        public TagEditModel(Tag edited, ITagEditCallback editCallback)
            : base(edited)
        {
            this.editCallback = editCallback;

            this.Properties =
                new CommitableObservableCollection<FacetPropertyEditModel>(edited.Facet.Properties.Select(p => new FacetPropertyEditModel(this, p)));

            this.CreatePropertyCommand = new RelayCommand(this.CreatePropertyExecuted);
            this.RemovePropertyCommand = new RelayCommand<FacetPropertyEditModel>(this.RemovePropertyExecuted);
        }

        #region Facet has observable collection of facet properties

        public CommitableObservableCollection<FacetPropertyEditModel> Properties { get; private set; }

        #endregion Facet has observable collection of facet properties

        #region Commands

        private void CreatePropertyExecuted()
        {
            this.Properties.Add(new FacetPropertyEditModel(this, new FacetProperty("new property")));
        }

        public ICommand CreatePropertyCommand { get; }

        private void RemovePropertyExecuted(FacetPropertyEditModel property)
        {
            this.Properties.Remove(property);
        }

        public ICommand RemovePropertyCommand { get; }

        #endregion Commands

        protected override void Commit()
        {
            this.Properties.Commit(
                onAdd: p => this.Model.Facet.Properties.Add(p.Model),
                onRemove: p => this.Model.Facet.Properties.Remove(p.Model));
            this.Properties.ForEach(p => p.CommitCommand.Execute(null));
            base.Commit();
            this.editCallback.Commit(this.Model);
        }

        protected override bool CanCommit()
        {
            if (this.HasErrors)
                return false;
            if (base.CanCommit())
                if (this.Properties.All(p => p.CommitCommand.CanExecute(null)))
                    return this.editCallback.CanCommit(this);
            return false;
        }

        protected override void Rollback()
        {
            this.Properties =
                new CommitableObservableCollection<FacetPropertyEditModel>(this.Model.Facet.Properties.Select(p => new FacetPropertyEditModel(this, p)));
            base.Rollback();
            this.editCallback.Rollback(this.Model);
        }

        #region Implement Validate

        protected override void Validate()
        {
            this.HasErrors = false;
            this.NameError = this.editCallback.Validate(this);

            // validate the repo data
            if (!string.IsNullOrEmpty(this.NameError))
            {
                this.HasErrors = this.HasErrors || true;
            }

            // validate the local data
            if (string.IsNullOrEmpty(this.Name))
            {
                this.HasErrors = true;
                this.NameError = "Tag name must not be empty";
            }

            // this has side effect to the editor
            this.CommitCommand.RaiseCanExecuteChanged();
        }

        #endregion Implement Validate
    }
}