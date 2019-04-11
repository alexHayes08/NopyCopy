# NopyCopy

> While debugging, updates files that are copied to the output directory upon
> saving them.

## Reason
Working with frameworks like [NopCommerce](https://www.nopcommerce.com/) have a
workflow where viewing updates to view files require the developer to restart
the debugging session. This extension allows for developers to remain debugging
while making view changes.

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
	'CopyToOutputDirectory' set to 'Copy always' or 'Copy if newer'.

3. Will only save files if they're saved from visual studio. If an external
	application modifies the file it won't be copied to the output directory.
