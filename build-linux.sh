TARGET=../../ClassiCube/Server

dotnet publish -c Release -o out/linux_x64 --runtime linux-x64
cp out/linux_x64/BanchoSpleef.dll $TARGET/plugins
