import 'dotnet.justfile'

format: (format-solution "NorthSouthSystems.Search.slnx")

deploy: tools (publish "https://api.nuget.org/v3/index.json")
