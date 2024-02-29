dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true --self-contained false 
cp -v ./bin/Release/net7.0/linux-x64/publish/paka /bin/paka
cp -v ./bin/Release/net7.0/linux-x64/publish/paka ./dist/paka

cp -v ./paka-update.sh /bin/paka-update
cp -v ./paka-update-formula.sh /bin/paka-update-formula

7z a ./dist/paka.7z ./dist/paka 