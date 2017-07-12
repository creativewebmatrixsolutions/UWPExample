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
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using PlayFab.ClientModels;

namespace SDKTemplate
{
    public sealed partial class MainPage : Page
    {
        private string userId;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            userId = ApplicationData.Current.LocalSettings.Values["userId"] as string ?? string.Empty;

            UpdateUI();
        }

        private void UpdateUI()
        {
            RegisteredUserName.Text = userId;
            if (userId.Length != 0)
            {
                SignInWithHelloContent.Visibility = Visibility.Visible;
                SignInWithPasswordContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                SignInWithHelloContent.Visibility = Visibility.Collapsed;
                SignInWithPasswordContent.Visibility = Visibility.Visible;
            }
        }

        private async void Unregister()
        {
            // Don't let the user try a new operation while we are busy with this one.
            SignInWithHelloButton.IsEnabled = false;
            SignInAsSomeoneElseButton.IsEnabled = false;

            // Only unlink and delete credentials if removing the account from the device.
            // If just "logging out" leave these intact.
            //var unlink = await PlayFabWindowsHello.UnlinkAccount(userId);

            //if (unlink)
            //{
                // Remove the credential from the key credential manager
                // await Util.TryDeleteCredentialAccountAsync(userId);
            //}

            // Remove our app's knowledge of the user.
            ApplicationData.Current.LocalSettings.Values.Remove("userId");
            userId = string.Empty;

            SignInWithHelloButton.IsEnabled = true;
            SignInAsSomeoneElseButton.IsEnabled = true;

            UpdateUI();
        }

        // Signing in requires a non-blank user name and password.
        private void OnSignInInfoChanged()
        {
            SignInWithPasswordButton.IsEnabled = !string.IsNullOrWhiteSpace(UserNameTextBox.Text);
        }

        private async void SignInWithPassword()
        {
            // Perform traditional standard authentication here.
            // Our sample accepts any userid and password.

            // Next, see if we can offer to switch to Windows Hello sign-in.
            // Check if the device is capable of provisioning Microsoft Passport key credentials and
            // the user has set up Windows Hello with a PIN on the device.
            if (await KeyCredentialManager.IsSupportedAsync())
            {
                // Navigate to Enable Hello Page, passing the account ID (username) as a parameter
                string accountID = UserNameTextBox.Text;
                Frame.Navigate(typeof(EnableHelloPage), accountID);
            }
            else
            {
                // Windows Hello is not available, so go directly to the signed-in state.
                // For the purpose of this sample, we will display a message to indicate
                // that this code path has been reached.
                MessageDialog message = new MessageDialog("Sample note: Windows Hello is not set up");
                await message.ShowAsync();
                return;
            }
        }


        private async void SignInWithHello()
        {
            ProgressRing.IsActive = true;

            // Don't let the user try a new operation while we are busy with this one.
            SignInWithHelloButton.IsEnabled = false;
            SignInAsSomeoneElseButton.IsEnabled = false;

            bool result = await PlayFabWindowsHello.SignInWithHelloAsync(userId);

            ProgressRing.IsActive = false;
            SignInWithHelloButton.IsEnabled = true;
            SignInAsSomeoneElseButton.IsEnabled = true;

            if (result)
            {
                Frame.Navigate(typeof(AccountOverviewPage));
            }
            else
            {
                MessageDialog message = new MessageDialog("Login with Windows Hello failed.");
                await message.ShowAsync();
            }
        }
    }
}
