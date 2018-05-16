# VSTS Tools
A set of useful tools and extensions for VSTS

## Variable Group Copier
A console application that clones a variable group.

Usage:
`AssureCare.VstsTools.VariableGroupCopier.exe [source_project [source_group [target_project [target_group [user_access_token [override_existent_target [vsts_account]]]]]]]`

All parameters are optional. Any missing parameters will be prompted by the app.
The defaults for the project names, access token, and account name can be set in app.config settings.

| Parameter Name | Description |
|----------------|-------------|
|source_project| The name of the project from where to copy the variable group|
|source_group| The name of the variable group to be copied|
|target_project|The name of the project to copy the variable group to|
|target_group|The name of the new variable group |
|user_access_token| Personal access token of a user that has permission for both project|
|override_existent_target| Y to override an existent target group <br/> N to skip it |
|vsts_account| VSTS account name |

