set shell := ["pwsh", "-nop", "-c"]

coverage_path := ".coverage"
installer_path := "artifacts"
configuration := "Debug"

[private]
default:
    @just --list --unsorted

# clean all object and binary files along with test output
clean:
    #!pwsh -nop
    foreach ($path in @(
        '.coverage'
        'src/*/TestResults'
        'src/*/bin/{{ configuration }}'
        'src/*/obj/{{ configuration }}'
    )) {
        if (Test-Path -Path $path) {
            Remove-Item -Path $path -Recurse -Force
        }
    }

# create the application icon
icon:
    #!pwsh -nop
    Set-Location -Path src/TotalMixVC/Icons
    foreach ($size in @(16, 32, 48, 128, 256)) {
        Write-Host "Rendering icon as PNG with dimensions ${size}x${size} using Inkscape"
        inkscape -w $size -h $size -o "${size}.png" TotalMixVC.svg
    }
    Write-Host 'Converting the PNGs to an ICO file using ImageMagick'
    magick convert 16.png 32.png 48.png 128.png 256.png TotalMixVC.ico
    Write-Host 'Cleaning up rendered PNG files'
    Remove-Item -Path *.png

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
        /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
    dotnet reportgenerator \
        -reports:src/*/coverage.opencover.xml \
        "-targetdir:{{ coverage_path }}" \
        "-reporttypes:Cobertura;lcov;Html" \
        -filefilters:-*.g.cs

# publish the application ready for distribution
publish: test
    dotnet publish \
        src/TotalMixVC --configuration {{ configuration }} --runtime win-x64 --self-contained

# create an installer for distribution
distribute: publish
    iscc "/O{{ installer_path }}" /DAppBuildConfiguration={{ configuration }} TotalMixVC.iss
