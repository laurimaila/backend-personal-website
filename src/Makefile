export PATH := $(shell go env GOPATH)/bin:$(PATH)

.PHONY: proto

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
