#/bin/bash

dotnet build -c Release

cp bin/Release/net7.0/cs-wrapper.dll lib/net7.0

dotnet pack -c Release -p:NuspecFile=cs-wrapper.nuspec