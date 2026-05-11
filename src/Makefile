export PATH := $(shell go env GOPATH)/bin:$(PATH)

.PHONY: proto migration migration-remove migration-list

# Usage: make migration name=AddSomeTable
migration:
	dotnet ef migrations add $(name) --project backend/backend.csproj --output-dir Data/Migrations

# Usage: make migration-remove
migration-remove:
	dotnet ef migrations remove --project backend/backend.csproj

# Usage: make migration-list
migration-list:
	dotnet ef migrations list --project backend/backend.csproj

proto:
	@echo "Generating Go code..."
	mkdir -p physics-service/pb
	protoc --proto_path=protos \
		--go_out=physics-service/pb --go_opt=paths=source_relative \
		--go-grpc_out=physics-service/pb --go-grpc_opt=paths=source_relative \
		physics.proto

	@echo "Building C# backend to regenerate client..."
	dotnet build backend/backend.csproj --nologo --verbosity quiet
	@echo "Done."
