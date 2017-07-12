//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
// Please refer to the Microsoft Passport and Windows Hello
// whitepaper on the Windows Dev Center for a complete
// explanation of Microsoft Passport and Windows Hello
// implementation: 
// http://go.microsoft.com/fwlink/p/?LinkId=522066
//*********************************************************
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using PlayFab;
using PlayFab.ClientModels;

namespace SDKTemplate
{
    public sealed partial class EnableHelloPage : Page
    {
        private string userId;

        public EnableHelloPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            userId = e.Parameter.ToString();
            WelcomeUserId.Text = userId;
        }

        private void SkipSetup()
        {
            // The user already signed in on the previous page, so we can go directly to the signed-in state.
            Frame.Navigate(typeof(AccountOverviewPage));
        }

        private async void StartUsingWindowsHello()
        {
            ProgressRing.IsActive = true;
            LaterButton.IsEnabled = false;
            StartUsingWindowsHelloButton.IsEnabled = false;

            var register = await PlayFabWindowsHello.RegisterWindowsHello(userId);

            // Registration successful. Continue to the signed-in state.
            if (register)
                Frame.Navigate(typeof(AccountOverviewPage));

            ProgressRing.IsActive = false;
            LaterButton.IsEnabled = true;
            StartUsingWindowsHelloButton.IsEnabled = true;
        }


        private async void LaunchHyperlink(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var target = (string)element.Tag;
            await Windows.System.Launcher.LaunchUriAsync(new Uri(target));

        }
    }
}
