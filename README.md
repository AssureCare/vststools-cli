# VSTS Tools
A set of useful command-line tools and extensions for VSTS

## Variable Group Copier
A CLI tool that copies or exports/imports VSTS variable groups

Usage:
`AssureCare.VstsTools.VariableGroupCopier.exe [source_location [source_group [target_project [target_group [user_access_token [override_existent_target [vsts_account]]]]]]]`

All parameters are optional. Any missing parameters will be prompted by the app.
The defaults for the project names, access token, and account name can be set in app.config settings.

| Parameter Name | Description |
|----------------|-------------|
|source_project| The name of the project from where to copy the variable group or `/file` for imports|
|source_group| The name of the variable group to be copied or `*` to copy all or folder or file path for imports|
|target_project|The name of the project to copy the variable group to or `/file` for exports|
|target_group|The name of the new variable group or * to use the same name as source or folder name for exports |
|user_access_token| Personal access token of a user that has permission for both project|
|override_existent_target| Y to override an existent target group <br/> N to skip it |
|vsts_account| VSTS account name |

