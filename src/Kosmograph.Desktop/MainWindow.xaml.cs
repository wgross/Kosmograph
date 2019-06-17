﻿using Kosmograph.Desktop.ViewModel;
using Kosmograph.LiteDb;
using Kosmograph.Messaging;
using Kosmograph.Model;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Kosmograph.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Loaded += this.MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel == null)
            {
                this.CreateNewModel();
                if (this.ViewModel == null)
                    if (Application.Current != null)
                        Application.Current.Shutdown();
            }

            //new Kosmograph.Desktop.Graph.View.GraphXViewerWindow
            //{
            //    DataContext = this.ViewModel.Graph,
            //    Owner = this,
            //    Left = this.Left,
            //    Top = this.Top + this.Height,
            //    Width = this.Width,
            //    Height = this.Height
            //}.Show();
        }

        private void CreateNewModel()
        {
            var model = new KosmographModel(new KosmographLiteDbPersistence(KosmographMessageBus.Default));
            var tag1 = model.Tags.Upsert(new Tag("t1", new Facet("facet", new FacetProperty("p1"))));
            var tag2 = model.Tags.Upsert(new Tag("t2", new Facet("facet", new FacetProperty("p2"))));
            var entity1 = model.Entities.Upsert(new Entity("e1", tag1));
            var entity2 = model.Entities.Upsert(new Entity("e2"));
            //var entity3 = model.Entities.Upsert(new Entity("entity3"));
            //var entity4 = model.Entities.Upsert(new Entity("entity4", tag1));
            var relationship1 = model.Relationships.Upsert(new Relationship("relationship1", entity1, entity2, tag2));
            // var relationship2 = model.Relationships.Upsert(new Relationship("relationship2", entity2, entity3, tag2));
            var viewModel = new KosmographViewModel(model);
            viewModel.FillAll();
            this.ViewModel = viewModel;

            CommandManager.InvalidateRequerySuggested();
        }

        public KosmographViewModel ViewModel
        {
            get
            {
                return (KosmographViewModel)this.DataContext;
            }
            set
            {
                this.ViewModel?.Dispose();
                this.DataContext = value;
                //this.ViewModel.PropertyChanged += this.Value_PropertyChanged;
            }
        }

        //private void Value_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName.Equals(nameof(KosmographViewModel.DeletingEntity)))
        //    {
        //        if (this.ViewModel.DeletingEntity is null)
        //        {
        //            this.DeleteEntityWithRelationshipsDialog?.Close();
        //            this.DeleteEntityWithRelationshipsDialog = null;
        //        }
        //        else
        //        {
        //            this.DeleteEntityWithRelationshipsDialog = new DeleteEntityWithRelationshipsDialog
        //            {
        //                DataContext = this.ViewModel.DeletingEntity,
        //                Owner = this
        //            };
        //            this.DeleteEntityWithRelationshipsDialog.ShowDialog();
        //        }
        //    }
        //}

        private void NewGraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Choose a directory and file name to create new graph...";
            openFileDialog.DefaultExt = "kg";
            openFileDialog.CheckFileExists = false;
            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            this.ViewModel = new KosmographViewModel(new KosmographModel(new KosmographLiteDbPersistence(File.Create(openFileDialog.FileName))));
            this.ViewModel.FillAll();

            //using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            //{
            //    if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //        ;
            //}
        }

        private void OpenGraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Choose a Kosmograph file to open...";
            openFileDialog.DefaultExt = "kg";
            openFileDialog.CheckFileExists = true;
            if (!(openFileDialog.ShowDialog() ?? false))
                return;

            this.ViewModel = new KosmographViewModel(new KosmographModel(new KosmographLiteDbPersistence(File.Open(openFileDialog.FileName, FileMode.Open))));
            this.ViewModel.FillAll();
        }
    }
}