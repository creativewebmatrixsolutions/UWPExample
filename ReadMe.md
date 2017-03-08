## PlayFab & Windows Hello (Microsoft Passport for UWP) Sample Application

This Universal Windows Application (UWP) shows off how to use PlayFab authentiation with Microsoft Passport for UWP.

Before launching the application you must set the Title ID in `App.InitializePlayFab()`

### General Flow

#### Registration

- Data needed:
	- Windows: The user name

- Call `KeyCredentialManager.RequestCreateAsync` to generate a new public key for this user.
- Call `CryptographicBuffer.EncodeToBase64String` to convert the `IBuffer` from above to a string.
- Call `PlayFabClientAPI.RegisterWithWindowsHelloAsync` with the base 64 encoded public key from above.
- If Register is successful use `HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256)` to create a hash provider and hash the public key by calling `hashProvider.HashData(publicKey)`. Convert the hashed public key to a base 64 encoded string (`CryptographicBuffer.EncodeToBase64String(publicKeyHash)`) and store this string and the user name in the application settings (`ApplicationData.Current.LocalSettings.Values["publicKeyHint"]`). This public key hint is used to log back in. The username should also be stored in the user's local settings as well for simpler login.

From this point the SDK will automatically be authenticated until the session ticket expires.

#### Login

- Data needed:
	- PlayFab: The public key hint
	- Windows: The user name

- Call `PlayFabClientAPI.GetWindowsHelloChallengeAsync` to create a signing challenge.
- Call `CryptographicBuffer.DecodeFromBase64String` to create an IBuffer for the KeyCredentialManager to have the user sign.
- Call `var retrieveResult = await KeyCredentialManager.OpenAsync(userId)` to create a key signing service
- Get the credential for this user `var userCredential = retrieveResult.Credential`
- Call `await userCredential.RequestSignAsync(challengeBuffer)` to have Windows request the user sign the server's challenge for this user

### Sample methods that call PlayFab

 - Registration
	- `EnableHelloPage` - `StartUsingWindowsHello`
		- Generate Passport private key
		- Register Passport private key with PlayFab servers to create an account via `RegisterPassportCredentialWithServerAsync`
 - Login
	- `MainPage` - `SignInWithHelloAsync`
		- Get server challenge string
		- Request user sign challenge
		- Login to PlayFab
