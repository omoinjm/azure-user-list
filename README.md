---
title: "Get list of users on Azure Active Directory using Microsoft Graph API"
permalink: /
layout: default
---

# Get list of users on Azure Active Directory using Microsoft Graph API

Documentation Links:
  - [Application Portal setup](https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#app-registration-app-objects-and-service-principals)
  - [Azure.Identity](https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider)
  - [Create Console App](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal)

1. Obtain a valid authentication token:
  
  - Navigate to `Azure Active Directory`
    
    ![Azure AD](./images/1.png) 
 
  - Create a new `App registration`

    ![Azure AD](./images/2.png)

    ![Azure AD](./images/3.png)

    ![Azure AD](./images/4.png)

  - Add `API Permissions` for the Graph resources you'd like to access
    
    - Click on the newly created application

      ![Azure AD](./images/5.png)

      ![Azure AD](./images/6.png)

    - Add the following permissions

      ![Azure AD](./images/7.png)

    - Click on `Add a permission`

    - Navigate to `Microsoft APIs` -> `Microsoft Graph` -> `Application permissions`

    - Search for `User`

      ![Azure AD](./images/8.png)

    - Click `User.Read.All` and `User.ReadBasic.All`
            
      ![Azure AD](./images/9.png)

    - Navigate to `Microsoft APIs` -> `Microsoft Graph` -> `Delegate permissions`

    - Search `User`

    - Click on `User.Read` and `User.ReadBasic.All` -> `Add Permission`
           
    - Make sure to click `Grant admin consent`

      ![Azure AD](./images/10.png)

2. Creating application

  - Navigate to `Certificates & Secrets` and create a `New client secrets`

  - Copy the `Value` and paste it into notepad

    ![Azure AD](./images/11.png)

  - Go back to `Overview`

  - Copy `Application (client) ID` as well as `Directory (tenant) ID` and paste them into notepad

    ![Azure AD](./images/12.png) 

  - Go to this GitHub repository to find the code: `https://github.com/omoinjm/azure-user-list`

    OR

  - Create new Console Application:
    
    ```bash
    dotnet new console
    ```

  - Install the following packages:
    
    ```bash
    dotnet add package DotNetEnv

    dotnet add package Microsoft.Graph.Auth --version 1.0.0-preview.7

    dotnet add package Microsoft.Graph

    dotnet add package Microsoft.Identity.Client
    ```
  - Insert code in Program.cs

    ![Azure AD](./images/Program.png)

  - Insert Environment Variables

    ![Env](./images/env.png)
    
