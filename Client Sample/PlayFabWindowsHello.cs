using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using PlayFab.ClientModels;

namespace SDKTemplate
{
    public static class PlayFabWindowsHello
    {
        public static async Task<bool> UnlinkAccount(string userId)
        {
            var retrieveResult = await KeyCredentialManager.OpenAsync(userId);

            if (retrieveResult.Status != KeyCredentialStatus.Success)
            {
                return false;
            }

            KeyCredential userCredential = retrieveResult.Credential;

            string publicKeyHint = CalculatePublicKeyHint(userCredential.RetrievePublicKey());

            var unlinkResult = await PlayFab.PlayFabClientAPI.UnlinkWindowsHelloAsync(new UnlinkWindowsHelloAccountRequest
            {
                PublicKeyHint = publicKeyHint
            });

            if (unlinkResult.Error != null)
            {
                MessageDialog message = new MessageDialog($"Error while un-linking: {unlinkResult.Error.Error} {unlinkResult.Error.ErrorMessage}");
                await message.ShowAsync();
            }

            return unlinkResult.Error == null;
        }

        public static async Task<bool> SignInWithHelloAsync(string userId, KeyCredentialRetrievalResult retrieveResult = null)
        {
            // Open the existing user key credential.
            if (retrieveResult == null)
                retrieveResult = await KeyCredentialManager.OpenAsync(userId);

            if (retrieveResult.Status != KeyCredentialStatus.Success)
            {
                return false;
            }

            KeyCredential userCredential = retrieveResult.Credential;

            string publicKeyHint = CalculatePublicKeyHint(userCredential.RetrievePublicKey());

            var challengeResult = await PlayFab.PlayFabClientAPI.GetWindowsHelloChallengeAsync(new GetWindowsHelloChallengeRequest
            {
                PublicKeyHint = publicKeyHint,
                TitleId = PlayFab.PlayFabSettings.TitleId
            });

            if (challengeResult.Error != null)
            {
                // TODO: Failed to get a challenge, handle the error
                MessageDialog message = new MessageDialog($"Error during getting challenge: {challengeResult.Error.Error}");
                await message.ShowAsync();
                return false;
            }

            // Sign the challenge using the user's KeyCredential.
            IBuffer challengeBuffer = CryptographicBuffer.DecodeFromBase64String(challengeResult.Result.Challenge);
            KeyCredentialOperationResult opResult = await userCredential.RequestSignAsync(challengeBuffer);

            if (opResult.Status != KeyCredentialStatus.Success)
            {
                MessageDialog message = new MessageDialog("Failed to have user sign the challenge string.");
                await message.ShowAsync();
                return false;
            }

            // Get the signature.
            IBuffer signatureBuffer = opResult.Result;

            // Send the signature back to the server to confirm our identity.
            // The publicKeyHint tells the server which public key to use to verify the signature.
            var loginResult = await PlayFab.PlayFabClientAPI.LoginWithWindowsHelloAsync(new LoginWithWindowsHelloRequest
            {
                ChallengeSignature = CryptographicBuffer.EncodeToBase64String(signatureBuffer),
                PublicKeyHint = publicKeyHint
            });

            if (loginResult.Error != null)
            {
                MessageDialog message = new MessageDialog($"Error during login: {loginResult.Error.Error}");
                await message.ShowAsync();
                return false;
            }

            return true;
        }
        public static async Task<bool> RegisterWindowsHello(string userId)
        {
            // Check if the user already exists and if so log them in.
            KeyCredentialRetrievalResult retrieveResult = await KeyCredentialManager.OpenAsync(userId);
            if (retrieveResult.Status == KeyCredentialStatus.Success)
            {
                return await SignInWithHelloAsync(userId, retrieveResult);
            }

            // Create the key credential with Passport APIs
            IBuffer publicKey = await CreatePassportKeyCredentialAsync(userId);
            if (publicKey != null)
            {
                // Register the public key and attestation of the key credential with the server
                // In a real-world scenario, this would likely also include:
                // - Certificate chain for attestation endorsement if available
                // - Status code of the Key Attestation result : Included / retrieved later / retry type
                if (await RegisterPassportCredentialWithServerAsync(publicKey, userId))
                {
                    // Remember that this is the user whose credentials have been registered
                    // with the server.
                    ApplicationData.Current.LocalSettings.Values["userId"] = userId;

                    // Registration successful. Continue to the signed-in state.
                    return true;
                }
                else
                {
                    // Delete the failed credentials from the device.
                    await Util.TryDeleteCredentialAccountAsync(userId);

                    MessageDialog message = new MessageDialog("Failed to register with the server.");
                    await message.ShowAsync();

                }
            }

            return false;
        }

        private static string CalculatePublicKeyHint(IBuffer publicKey)
        {
            HashAlgorithmProvider hashProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            IBuffer publicKeyHash = hashProvider.HashData(publicKey);
            return CryptographicBuffer.EncodeToBase64String(publicKeyHash);
        }

        private static async Task<IBuffer> CreatePassportKeyCredentialAsync(string userId)
        {
            // Create a new KeyCredential for the user on the device.
            KeyCredentialRetrievalResult keyCreationResult = await KeyCredentialManager.RequestCreateAsync(userId, KeyCredentialCreationOption.ReplaceExisting);
            if (keyCreationResult.Status == KeyCredentialStatus.Success)
            {
                // User has authenticated with Windows Hello and the key credential is created.
                KeyCredential userKey = keyCreationResult.Credential;
                return userKey.RetrievePublicKey();
            }
            else if (keyCreationResult.Status == KeyCredentialStatus.NotFound)
            {
                MessageDialog message = new MessageDialog("To proceed, Windows Hello needs to be configured in Windows Settings (Accounts -> Sign-in options)");
                await message.ShowAsync();

                return null;
            }
            else if (keyCreationResult.Status == KeyCredentialStatus.UnknownError)
            {
                MessageDialog message = new MessageDialog("The key credential could not be created. Please try again.");
                await message.ShowAsync();

                return null;
            }

            return null;
        }

        private static async Task<bool> RegisterPassportCredentialWithServerAsync(IBuffer publicKey, string userId)
        {
            // Include the name of the current device for the benefit of the user.
            // The server could support a Web interface that shows the user all the devices they
            // have signed in from and revoke access from devices they have lost.

            var hostNames = NetworkInformation.GetHostNames();
            var localName = hostNames.FirstOrDefault(name => name.DisplayName.Contains(".local"));
            string computerName = localName.DisplayName.Replace(".local", "");

            var registerResult = await PlayFab.PlayFabClientAPI.RegisterWithWindowsHelloAsync(new RegisterWithWindowsHelloRequest
            {
                DeviceName = computerName,
                PublicKey = CryptographicBuffer.EncodeToBase64String(publicKey),
                UserName = userId
            });

            if (registerResult.Error != null)
                return false;

            return true;
        }
    }
}
