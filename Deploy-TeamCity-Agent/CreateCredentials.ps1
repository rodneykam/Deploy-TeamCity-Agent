Login-AzureRmAccount -Subscription a646d321-18de-4209-a895-5c7dec3a9ca0

$passwd = ConvertTo-SecureString "R3l@yH3@lth" -AsPlainText -Force

# Create the service principal (use a strong password)
$sp = New-AzureRmADServicePrincipal -DisplayName "AzureTeamCityDeploy" -Password $passwd

# Give it the permissions it needs...
New-AzureRmRoleAssignment -ServicePrincipalName $sp.ApplicationId -RoleDefinitionName Contributor

# Display the Application ID, because we'll need it later.
$sp | Select DisplayName, ApplicationId