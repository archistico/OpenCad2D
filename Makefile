.PHONY: run build test check clean zip z

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

zip:
ifeq ($(OS),Windows_NT)
	@powershell -NoProfile -ExecutionPolicy Bypass -Command "$$date = Get-Date -Format 'yyyyMMdd'; $$name = $${date} + '_OpenCad2d.zip'; if (Test-Path $$name) { Remove-Item $$name -Force }; Compress-Archive -Path 'docs','src','tests','OpenCad2D.sln','README.md','THIRD-PARTY-NOTICES.md' -DestinationPath $$name -Force; Write-Host ('Created ' + $$name)"
else
	@name="$$(date +%Y%m%d)_OpenCad2d.zip"; rm -f "$$name"; zip -r "$$name" docs src tests OpenCad2D.sln README.md THIRD-PARTY-NOTICES.md; echo "Created $$name"
endif

z: clean zip

publish:
	dotnet publish src\OpenCad2D.App\OpenCad2D.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\OpenCad2D-win-x64 /p:DebugType=None /p:DebugSymbols=false