set shell := ["pwsh", "-nop", "-c"]

coverage_path := ".coverage"
installer_path := "artifacts"

[private]
default:
    @just --list --unsorted

# clean all object and binary files along with test output
clean cfg="Debug":
    #!pwsh -nop
    foreach ($path in @(
        '.coverage'
        'src/*/TestResults'
        'src/*/bin/{{ cfg }}'
        'src/*/obj/{{ cfg }}'
    )) {
        if (Test-Path -Path $path) {
            Remove-Item -Path $path -Recurse -Force
        }
    }

# restore the required tools and project dependencies
restore:
    dotnet tool restore
    dotnet restore

# build the application
build cfg="Debug": restore
    dotnet build --configuration {{ cfg }} --no-restore

# test the application and produce a coverage report
test cfg="Debug": (build cfg)
    dotnet test \
        --configuration {{ cfg }} --logger xunit --verbosity normal --no-build \
        /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
    dotnet reportgenerator \
        -reports:src/*/coverage.opencover.xml \
        "-targetdir:{{ coverage_path }}" \
        "-reporttypes:Cobertura;lcov;Html"

# publish the application ready for distribution
publish cfg="Debug": (test cfg)
    dotnet publish src/TotalMixVC --configuration {{ cfg }} --runtime win-x64 --self-contained

# create an installer for distribution
distribute cfg="Debug": (publish cfg)
    dotnet iscc "/O{{ installer_path }}" /DAppBuildConfiguration={{ cfg }} TotalMixVC.iss
