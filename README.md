In order to setup the server to run it locally follow the following steps:

1. Delete `local.settings.json` file if it exists.
2. In terminal change directory to the project root directory.
3. Log into your Azure account:
```ps
az login
```
4. Switch to **ShopServicesSubscription** subscription:
```ps
az account set --subscription "ShopServicesSubscription"
```
5. Fetch the configuration of `func-shop-services-003`:
```ps
func azure functionapp fetch-app-settings func-shop-services-003
```
