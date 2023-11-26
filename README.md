# Play.Identity
Play Economy Identity microservice

## Create and publish package
Don't forget to change the version into the kubernetes\identity.yaml
```powershell
$version="1.0.11"
$owner="wallawastoo"
$gh_pat="[PAT HERE]"

dotnet pack src\Play.Identity.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/play.identity -o ..\packages

dotnet nuget push ..\packages\Play.Identity.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image
```powershell
$env:GH_OWNER="wallawastoo"
$env:GH_PAT="[PAT HERE]"
$appname="waplayeconomy"
$resourcegroup="playeconomy"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$appname.azurecr.io/play.identity:$version" .
```

## Run the docker image
```powershell
$adminPass="[PASSWORD HERE]"
$cosmosDbConnString="[CONN HERE]"
$serviceBusConnString="[CONN STRING HERE]"
docker run -it --rm -p 5002:5002 --name identity -e MongoDbSettings__ConnectionString=$cosmosDbConnString -e ServiceBusSettings__ConnectionString=$serviceBusConnString -e ServiceSettings__MessageBroker="SERVICEBUS" -e IdentitySettings__AdminUserPassword=$adminPass play.identity:$version
```

## Publishing the docker image
```powershell
az acr login --name $appname
docker push "$appname.azurecr.io/play.identity:$version"
```

## Create the Kubernetes namespace
```powershell
$namespace="identity"
kubectl create namespace $namespace
```

To check the pods running
```powershell
kubectl get pods -n $namespace
```

## Check the service IP and infos
```powershell
kubectl get services -n $namespace
```

## Creating the Azure Managed Identity and granting it access to Key Vault secrets
```powershell
az identity create --resource-group $resourcegroup --name $namespace
$IDENTITY_CLIENT_ID=az identity show -g $resourcegroup -n $namespace --query clientId -otsv
az keyvault set-policy -n $appname --secret-permissions get list --spn $IDENTITY_CLIENT_ID
```

## Estabilish the federated identity credential
```powershell
$AKS_OIDC_ISSUER=az aks show -n $appname -g $resourcegroup --query "oidcIssuerProfile.issuerUrl" -otsv

az identity federated-credential create --name $namespace --identity-name $namespace --resource-group $resourcegroup --issuer $AKS_OIDC_ISSUER --subject "system:serviceaccount:${namespace}:${namespace}-serviceaccount"
```

## Install the helm chart
```powershell
$helmUser=[guid]::Empty.Guid
$helmPassword=az acr login --name $appname --expose-token --output tsv --query accessToken
helm registry login "$appname.azurecr.io" --username $helmUser --password $helmPassword

$chartVersion="0.1.0"
helm upgrade identity-service oci://$appname.azurecr.io/helm/microservice --version $chartVersion -f .\helm\values.yaml -n $namespace --install
```

## Required repository secrets for GitHub workflow

AZURE_CLIENT_ID: From AAD app Registration
AZURE_SUBSCRIPTION_ID: From Azure portal subscription
AZURE_TENANT_ID: From AAD properties page