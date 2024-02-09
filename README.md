<h1 align="center">My Anime Sync</h1>

## About the Plugin
This is a simple plugin for Jellyfin to update watch status on <a src="https://myanimelist.net/">MyAnimeList.net</a><br/>
The goal of the implementation is to solely use the API for both gathering information about the anime and updating the user list.<br/>

## Important Notes
The refresh token for the API is only valid for a period of 1 month.<br/>
If you ever stop the server for more than one month, the refresh token will be invalid and you will have to redo the authentication process.<br/>
Otherwise, tokens are automatically refreshed when either required or if the access token is 7+ days old.<br/>
A task is executed every day, validating if the tokens need to be refreshed. As long as the server is running, your tokens should never expire.<br/>

If you ever encounter issues with a specific anime, feel free to create a ticket. The goal here is for the implementation to work with every single anime listed on MyAnimeList.<br/>

## How to Install the Plugin
1. Important notes:
   - I highly recommend running the plugin using the Jellyfin stable build.<br/>
   - If you want to use the plugin with the unstable version of Jellyfin, be aware that you might need to manually build the plugin. The release dll might not work on the unstable version.<br/>
   - You might also encounter issues when using the plugin with the unstable version even if it was built manually. I would not recommend it unless absolutely necessary.

2. Install from Jellyfin Catalog
   1. Go to Dashboard -> Plugins -> Repositories and add a custom repository
      - For the repository url, use https://raw.githubusercontent.com/iankiller77/MyAnimeSync/main/manifest.json
      - The repository name can be anything
   2. Go to catalog and look for MyAnimeSync in the General plugin list.
   3. Install the plugin and restart the jellyfin server.

2. Manual Install:
   - To manually install the plugin, simply copy the files in the Jellyfin plugin folder. Default should be %UserProfile%\AppData\Local\jellyfin\plugins

## How to Setup the Plugin

### Prepare your MyAnimeList Account for Authentication
1. Go into Account Settings -> API
2. Click the create ID button and fill the required information:
   - For app type, make sure to select web.
   - App Redirect URL must be the Redirect Url specified in MyAnimeSync plugin configuration page.
      * Make sure that the Jellyfin Url specified on the plugin configuration is the proper URL (or IP address) used to access your Jellyfin server. Otherwise, update it.
   
3. Submit the new ID.
4. Return to the API and click on edit for the ID you just created to retrieve the Client ID and Client Secret.

### Finish Configuration on the Plugin Configuration Page
1. Fill the plugin configuration page with the Client ID and Client Secret.
2. Click on the Authenticate User button.
3. Copy API Url in your favourite web browser and allow the connection.
4. To make sure that the authentication process was a success, click on the Validate Configuration button. This process can take some time, but eventually a pop-up will appear with the result.<br/>
   If the authentication was not a success, start over the authentication process. Pay particular attention to the Jellyfin URL.

## How to Build and Test the Project Using VSCode
I recommend using the provided VSCode template. If you want to compile the project in VSCode, make sure that the directory for the server, client and plugin match the following tree.<br/>
```
.
├── Jellyfin
├── Jellyfin-web
└── MyAnimeSync
```
For simplicity I will provide the steps required to build both the Jellyfin server and the Jellyfin web application.<br/>

### Build Jellyfin Server
For my project I built the server with dotnet 8, I would recommend doing the same. Otherwise make sur you update the launch.json to use proper dotnet version.

1. Clone or download this repository.
   ```sh
   git clone https://github.com/Jellyfin/Jellyfin.git
   cd Jellyfin
   ```

2. Build the server

   ```sh
   dotnet build
   ```

### Build Jellyfin Web Client

#### Dependencies

- [Node.js](https://nodejs.org/en/download)
- npm (included in Node.js)

#### Build

1. Clone or download this repository.

   ```sh
   git clone https://github.com/Jellyfin/Jellyfin-web.git
   cd Jellyfin-web
   ```

2. Install build dependencies in the project directory.

   ```sh
   npm install
   ```

3. Build the client with source maps available.

   ```sh
   npm run build:development
   ```

### Build the Plugin and Start Jellyfin Server
To build the plugin and start the Jellyfin server simply press the Debug: Start Debugging shortcut (default is F5)<br/>
Building the plugin requires dotnet6.
