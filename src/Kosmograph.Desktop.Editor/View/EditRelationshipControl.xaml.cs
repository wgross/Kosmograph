﻿using Kosmograph.Desktop.Editors.ViewModel;
using Kosmograph.Model;
using System.Windows;
using System.Windows.Controls;

namespace Kosmograph.Desktop.Editors.View
{
    public partial class EditRelationshipControl : UserControl
    {
        public EditRelationshipControl()
        {
            this.InitializeComponent();
        }

        private RelationshipEditModel ViewModel => this.DataContext as RelationshipEditModel;

        private void editRelationshipControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Tag)))
            {
                this.ViewModel.AssignTagCommand.Execute(e.Data.GetData(typeof(Tag)));
            }
            e.Handled = true;
        }
    }
}