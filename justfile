set shell := ["fresh", "-c"]
set export

configuration := env("CONFIGURATION", "Debug")
architecture := env("ARCHITECTURE", "x64")
type := env("TYPE", "SelfContained")

[private]
default:
    @just --list --unsorted

# clean all object and binary files along with test output
clean:
    rm -rf \
        .coverage \
        src/*/bin/{{configuration}} \
        src/*/bin/{{configuration}}/*/TestResults \
        src/*/obj/{{configuration}}

# create the application icon
icon:
    #!fresh
    set -euxo pipefail

    file_names=()
    for size in 16 32 48 128 256; do
        file_name=$size.png
        inkscape -w $size -h $size -o $file_name TotalMixVC.svg
        file_names+=($file_name)
    done

    magick "${file_names[@]}" -compress none src/TotalMixVC/Icons/TotalMixVC.ico
    rm "${file_names[@]}"

# restore the required tools and project dependencies
restore:
    dotnet tool restore
    dotnet restore

# build the application
build: restore
    dotnet build --configuration {{configuration}} --no-restore

# run the unit tests
[arg("class", short, long, help="the class to filter on")]
[arg("method", short, long, help="the method to filter on")]
test class="" method="": build
    #!fresh
    set -euxo pipefail

    extra_args=()
    if [[ $class != '' ]]; then
        extra_args+=(--filter-class $class)
    fi
    if [[ $method != '' ]]; then
        extra_args+=(--filter-method $method)
    fi

    rm -f src/*/bin/{{configuration}}/*/TestResults/*.xml
    dotnet run \
        --project src/TotalMixVC.Tests \
        --configuration {{configuration}} \
        --no-build \
        --coverage \
        --coverage-output-format xml \
        "${extra_args[@]}"

# produce a coverage report for unit tests
report: test
    dotnet reportgenerator \
        -reports:src/*/bin/{{configuration}}/*/TestResults/*.xml \
        -targetdir:.coverage \
        -reporttypes:Cobertura,lcov,Html \
        -filefilters:-*.g.cs

# publish the application ready for distribution
publish: restore
    #!fresh
    set -euxo pipefail

    extra_args=()
    if [[ '{{type}}' == 'SelfContained' ]]; then
        extra_args+=(--self-contained)
    fi

    dotnet publish src/TotalMixVC \
        --configuration {{configuration}} \
        --runtime win-{{architecture}} \
        "${extra_args[@]}"

# create an installer for distribution
installer: publish
    iscc \
        /Oartifacts \
        /DAppBuildConfiguration={{configuration}} \
        /DAppBuildArchitecture={{architecture}} \
        /DAppBuildType={{type}} \
        TotalMixVC.iss
