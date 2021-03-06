﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;

namespace SlipStream.Client.Agos.UI
{
    public partial class DBCreationPage : Page
    {
        public DBCreationPage()
        {
            InitializeComponent();
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            app.MainPage.NavigateToByRelative("/DBManagement");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var model = (Models.DBCreationModel)this.DataContext;
            var app = (App)Application.Current;

            app.IsBusy = true;
            app.ClientService.CreateDatabase(model.ServerPassword, model.DBName, model.AdminPassword, (error) =>
            {
                app.IsBusy = false;
                app.MainPage.NavigateToByRelative("/DBManagement");
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new Models.DBCreationModel();
        }

    }
}
