---
title: "Azure User List - Fetch Azure AD Users via Microsoft Graph"
permalink: /
layout: default
---

# Azure User List - Production-Ready Azure Function

Fetches users from Azure Active Directory using Microsoft Graph API and persists them to a SQL Server database via stored procedures. Built with clean architecture principles, full dependency injection, and comprehensive security hardening.

## 📋 Quick Links

- [Azure AD App Registration Setup](https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#app-registration-app-objects-and-service-principals)
- [Microsoft Graph Authentication](https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS#client-credentials-provider)
- [Deploy Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal)
- [Architectural Guide](./docs/ARCHITECTURE_REFACTOR_GUIDE.md)

---

## 🏗️ Architecture

This project follows **clean architecture** principles with 4 distinct layers:

```
Functions (HTTP Entry Point)
    ↓
Services (Business Logic Orchestration)
    ↓
Infrastructure (Graph API + SQL Database)
    ↓
Core (Domain Models + Configuration)
```

**Key Features:**
- ✅ Full dependency injection (no static methods)
- ✅ Parameterized SQL queries (SQL injection prevention)
- ✅ Proper async/await throughout
- ✅ Centralized configuration with validation
- ✅ 100% testable (all interfaces mockable)
- ✅ Separation of concerns (Graph SDK isolated from business logic)

---

## 🔧 Project Structure

```
GetAzureADUsers/
├── Core/
│   ├── Configuration/          # Configuration interfaces & implementation
│   ├── Constants/              # Field selectors, constants
│   └── Models/                 # Domain models (AzureADUser, QueryResult)
├── Infrastructure/
│   ├── Graph/                  # Graph API authentication & user fetching
│   └── Persistence/            # SQL Server repository
├── Services/
│   └── AzureADUserService.cs   # Business logic orchestration
├── Functions/
│   └── HttpGetAzureADUsers.cs  # HTTP trigger entry point
└── GetAzureADUsers.csproj
```

---

## 🚀 Getting Started

### Step 1: Azure AD Setup

#### 1.1 Register Application in Azure AD

Navigate to **Azure Active Directory**:

![Azure AD](./docs/assets/1.png)

Click **New registration**:

![Azure AD](./docs/assets/2.png)

![Azure AD](./docs/assets/3.png)

![Azure AD](./docs/assets/4.png)

#### 1.2 Configure API Permissions

Click on your newly created application:

![Azure AD](./docs/assets/5.png)

![Azure AD](./docs/assets/6.png)

Add permissions for Microsoft Graph:

![Azure AD](./docs/assets/7.png)

Click **Add a permission** → **Microsoft APIs** → **Microsoft Graph** → **Application permissions**

Search for "User" and select **User.Read.All** and **User.ReadBasic.All**:

![Azure AD](./docs/assets/8.png)

![Azure AD](./docs/assets/9.png)

Also add **Delegated permissions** (search "User" → **User.Read** and **User.ReadBasic.All**):

Make sure to **Grant admin consent**:

![Azure AD](./docs/assets/10.png)

#### 1.3 Create Client Secret

Navigate to **Certificates & Secrets** and create a new client secret:

![Azure AD](./docs/assets/11.png)

Copy the **Value** and save it securely.

Navigate back to **Overview** and copy both:
- **Application (client) ID**
- **Directory (tenant) ID**

![Azure AD](./docs/assets/12.png)

---

### Step 2: Local Development Setup

#### 2.1 Clone Repository

```bash
git clone https://github.com/omoinjm/azure-user-list.git
cd azure-user-list
```

#### 2.2 Configure Environment Variables

Copy the template:

```bash
cp local.settings.json.template local.settings.json
```

Edit `local.settings.json` and add your credentials:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_TENANT_ID": "your-tenant-id",
    "AZURE_CLIENT_ID": "your-client-id",
    "AZURE_CLIENT_SECRET": "your-client-secret",
    "SQL_CONNECTION_STRING": "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;"
  }
}
```

#### 2.3 Install Dependencies

```bash
dotnet restore
```

#### 2.4 Build & Run Locally

```bash
dotnet build
dotnet run
```

The function will start on `http://localhost:7071`. Test with:

```bash
curl http://localhost:7071/api/users
```

---

### Step 3: Deploy to Azure

#### 3.1 Prerequisites

```bash
# Install Azure CLI
# https://learn.microsoft.com/en-us/cli/azure/install-azure-cli

az login
az account set --subscription "your-subscription-id"
```

#### 3.2 Create Resources

```bash
# Create resource group
az group create --name myResourceGroup --location eastus

# Create storage account (required by Azure Functions)
az storage account create \
  --name mystorageaccount \
  --resource-group myResourceGroup \
  --location eastus \
  --sku Standard_LRS

# Create Function App
az functionapp create \
  --resource-group myResourceGroup \
  --consumption-plan-location eastus \
  --name myFunctionApp \
  --storage-account mystorageaccount \
  --runtime dotnet-isolated \
  --runtime-version 8.0
```

#### 3.3 Configure Application Settings

```bash
az functionapp config appsettings set \
  --name myFunctionApp \
  --resource-group myResourceGroup \
  --settings \
    AZURE_TENANT_ID="your-tenant-id" \
    AZURE_CLIENT_ID="your-client-id" \
    AZURE_CLIENT_SECRET="your-client-secret" \
    SQL_CONNECTION_STRING="your-connection-string"
```

#### 3.4 Deploy

```bash
func azure functionapp publish myFunctionApp
```

---

## 📊 Data Flow

```
Client (HTTP GET /api/users)
    ↓
HttpGetAzureADUsers (orchestrates)
    ↓
AzureADUserService (business logic)
    ├─→ GraphAuthenticator (gets auth token)
    ├─→ GraphUserFetcher (fetches from Graph API)
    └─→ SqlAzureUserRepository (persists to SQL Server)
    ↓
Response (SyncResult: success/error, count, details)
```

---

## 🔒 Security Features

- ✅ **Parameterized SQL Queries** - Prevents SQL injection
- ✅ **Client Secret Credential** - Azure.Identity for secure authentication
- ✅ **No Credential Logging** - All secrets filtered from logs
- ✅ **Function-Level Authorization** - Restrict by IP/auth
- ✅ **Centralized Config Validation** - Fails fast if credentials missing
- ✅ **Async/Await Throughout** - No blocking operations

---

## 🧪 Testing

Unit test examples are provided in the documentation. To run tests:

```bash
# Create test project (if needed)
dotnet new xunit -n GetAzureADUsers.Tests

# Install test dependencies
cd GetAzureADUsers.Tests
dotnet add package Moq
dotnet add package xunit

# Run tests
dotnet test
```

See [Unit Test Examples](./docs/UNIT_TEST_EXAMPLES.md) for comprehensive patterns.

---

## 📚 Documentation

- **[ARCHITECTURE_REFACTOR_GUIDE.md](./docs/ARCHITECTURE_REFACTOR_GUIDE.md)** - Complete architectural guide with code examples
- **[UNIT_TEST_EXAMPLES.md](./docs/UNIT_TEST_EXAMPLES.md)** - Test patterns and mock examples
- **[CLEANUP_SUMMARY.md](./docs/CLEANUP_SUMMARY.md)** - What changed from original

---

## 🛠️ Configuration

All configuration is managed via **AzureADConfiguration.cs** and validated at startup:

| Variable | Required | Description |
|----------|----------|-------------|
| `AZURE_TENANT_ID` | Yes | Azure AD tenant ID |
| `AZURE_CLIENT_ID` | Yes | Application (client) ID |
| `AZURE_CLIENT_SECRET` | Yes | Client secret value |
| `SQL_CONNECTION_STRING` | Yes | SQL Server connection string |

Missing configuration throws `InvalidOperationException` on function startup.

---

## 📦 Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Graph | 4.50.0 | Azure AD user fetching |
| Azure.Identity | 1.8.0 | Secure credential handling |
| Microsoft.Data.SqlClient | Latest | Database access |
| Microsoft.Azure.Functions.Worker | Latest | Azure Functions runtime |

---

## 🚦 API Reference

### GET `/api/users`

**Request:**
```http
GET /api/users HTTP/1.1
Host: myFunctionApp.azurewebsites.net
```

**Response (200 OK):**
```json
{
  "isSuccess": true,
  "userCount": 42,
  "errorMessage": null,
  "timestamp": "2026-01-28T23:50:00Z"
}
```

**Response (500 Internal Server Error):**
```json
{
  "isSuccess": false,
  "userCount": 0,
  "errorMessage": "Failed to authenticate with Azure AD",
  "timestamp": "2026-01-28T23:50:00Z"
}
```

---

## 🆘 Troubleshooting

**"Invalid credentials" error:**
- Verify `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` are correct
- Check that client secret hasn't expired in Azure AD

**"Connection timeout" to SQL Server:**
- Verify SQL Server firewall allows Azure Function's IP
- Check `SQL_CONNECTION_STRING` is correct
- Verify stored procedure exists in database

**"User.Read.All permission not granted":**
- Ensure you clicked "Grant admin consent" in Azure AD
- Admin must approve permissions before first use

---

## 📄 License

See [LICENSE](./LICENSE) file.

---

## 🤝 Contributing

This project is production-ready. For changes:
1. Follow the existing architecture patterns
2. Add tests for new functionality
3. Verify async/await compliance
4. Use parameterized SQL queries

---

**Last Updated:** January 2026 | **Status:** Production Ready ✅
