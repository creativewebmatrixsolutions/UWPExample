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

using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using PlayFab.ClientModels;

namespace SDKTemplate
{
    public sealed partial class AccountOverviewPage : Page
    {
        public AccountOverviewPage()
        {
            InitializeComponent();
        }

        private async Task LoadUserDetailsAsync()
        {
            var userDetails = await PlayFab.PlayFabClientAPI.GetAccountInfoAsync(new GetAccountInfoRequest());

            UserName.Text = userDetails.Result.AccountInfo.Username;
            PlayFabId.Text = userDetails.Result.AccountInfo.PlayFabId;

            JsonData.Text = JsonConvert.SerializeObject(userDetails.Result, Formatting.Indented);
        }

        private void SignOut()
        {
            while (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void UserData_OnLoading(FrameworkElement sender, object args)
        {
            LoadUserDetailsAsync();
        }
    }
}
