#!/usr/bin/make -f
#SHELL:=/bin/bash

default: help
help: # Show help for each of the Makefile recipes.
	@grep -E '^[a-zA-Z0-9 -]+:.*#'  Makefile | sort | while read -r l; do printf "\033[1;32m$$(echo $$l | cut -f 1 -d':')\033[00m:$$(echo $$l | cut -f 2- -d'#')\n"; done

.PHONY: all
all:
	@echo "Usage: make [target]"
	@exit 0

.PHONY: ci-test
ci-test: ## Run tests in CI
	@dotnet run --configuration Release

.PHONY: build-alpha-package
build-alpha-package: # builds a package with a pre-release version suffix
	@dotnet pack src/AspNet.KeyCloak.DPoP/AspNet.KeyCloak.DPoP.csproj --version-suffix 1.0.0-alpha -o artifacts

.PHONY: build-package
build-package: # builds a package with the version from the project file
	@dotnet pack src/AspNet.KeyCloak.DPoP/AspNet.KeyCloak.DPoP.csproj -o artifacts

