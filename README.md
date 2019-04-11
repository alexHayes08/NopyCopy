# NopyCopy

> Copies any files that are copied to the output directory while debugging upon
> saving them.

## Reason
Working with frameworks like [NopCommerce](https://www.nopcommerce.com/) have a
workflow where making view changes is tedious since to apply the changes the
developer has to restart the debugging session which can take a while. This
extension allows for developers to remain debugging while making view changes.

## Common Issues
1. If working with NopCommerce 4.0 or greater, make sure the following is in
	the .csproj or else this extension won't be able to identify the output
	directory correctly.
```
<PropertyGroup>
	<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
</PropertyGroup>
```

2. Will only copy files while debugging and only files with the property
	'CopyToOutputDirectory' set to 'CopyToOutput' or 'CopyIfNewer'.

3. Will only save files if they're saved from visual studio. If an external
	application modifies the file it won't be copied to the output directory.
