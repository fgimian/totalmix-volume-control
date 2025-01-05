configuration := "Debug"

[private]
default:
    @just --list --unsorted

# clean all object and binary files along with test output
clean:
    rm -rf .coverage src/*/TestResults src/*/bin/{{ configuration }} src/*/obj/{{ configuration }}

# create the application icon
icon:
    inkscape -w 16 -h 16 -o 16.png TotalMixVC.svg
    inkscape -w 32 -h 32 -o 32.png TotalMixVC.svg
    inkscape -w 48 -h 48 -o 48.png TotalMixVC.svg
    inkscape -w 128 -h 128 -o 128.png TotalMixVC.svg
    inkscape -w 256 -h 256 -o 256.png TotalMixVC.svg
    magick 16.png 32.png 48.png 128.png 256.png -compress none src/TotalMixVC/Icons/TotalMixVC.ico
    rm *.png

# restore the required tools and project dependencies
restore:
    dotnet tool restore
    dotnet restore

# build the application
build: restore
    dotnet build --configuration {{ configuration }} --no-restore

# test the application and produce a coverage report
test: build
    dotnet test \
        --configuration {{ configuration }} --logger xunit --verbosity normal --no-build \
        --collect:"XPlat Code Coverage"
    dotnet reportgenerator \
        -reports:src/**/coverage.cobertura.xml \
        -targetdir:.coverage \
        "-reporttypes:Cobertura;lcov;Html" \
        -filefilters:-*.g.cs

# publish the application ready for distribution
publish: test
    dotnet publish \
        src/TotalMixVC --configuration {{ configuration }} --runtime win-x64 --self-contained

# create an installer for distribution
distribute: publish
    iscc //Oartifacts //DAppBuildConfiguration={{ configuration }} TotalMixVC.iss
