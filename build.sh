#!/usr/bin/env bash
# to properly set Travis permissions: https://stackoverflow.com/questions/33820638/travis-yml-gradlew-permission-denied
# git update-index --chmod=+x fake.sh
# git commit -m "permission access for travis"

set -eu
set -o pipefail

dotnet tool restore
dotnet paket install
# dotnet restore build.proj

if [ ! -f build.fsx ]; then
    dotnet fake run init.fsx
fi

dotnet fsi build.fsx $@
