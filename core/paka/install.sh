dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true --self-contained false 
doas cp -v ./bin/Release/net7.0/linux-x64/publish/paka /bin/paka
cp -v ./bin/Release/net7.0/linux-x64/publish/paka ./dist/paka