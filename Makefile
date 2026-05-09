.PHONY: run build test check clean

run:
	dotnet run --project src/OpenCad2D.App

build:
	dotnet build

test:
	dotnet test

check: build test

clean:
	@echo "Removing bin and obj folders..."
ifeq ($(OS),Windows_NT)
	@if exist src for /d /r src %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
	@if exist tests for /d /r tests %%d in (bin,obj) do @if exist "%%d" rmdir /s /q "%%d"
else
	find src tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
endif
	@echo "Clean completed."