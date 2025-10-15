# Set CONTAINER_PATH to current directory if not defined
CONTAINER_PATH ?= .

.PHONY: build clean deploy run help dev watch

# Project variables
PROJECT_NAME = VideoPlayer
IMAGE_NAME = csharp-builder
OUTPUT_DIR = ./bin
CONTAINER_NAME = csharp-builder

help:
	@echo "Available commands:"
	@echo "  make build   - Build the Docker image and compile the C# application"
	@echo "  make dev     - Run development mode with auto-rebuild on file changes"
	@echo "  make watch   - Alias for 'make dev'"
	@echo "  make deploy  - Copy the compiled binary from container to host"
	@echo "  make clean   - Remove build artifacts and Docker containers"
	@echo "  make run     - Run the Windows executable (requires Wine or Windows)"
	@echo "  make all     - Build and deploy in one step"

# Build the Docker image and compile the application
build:
	@echo "Building Docker image and compiling C# application..."
	@mkdir -p $(OUTPUT_DIR)
	docker build -t $(IMAGE_NAME) .
	docker run --rm -v $(CONTAINER_PATH)/src:/app/src -v $(CONTAINER_PATH)/bin:/app/bin $(IMAGE_NAME) /app/build.sh
	@echo "Binary deployed to $(OUTPUT_DIR)/"
	@echo "You can now run the executable on Windows: $(OUTPUT_DIR)/$(PROJECT_NAME).exe"

# Extract the compiled binary from the container (alias for backward compatibility)
deploy:
	@echo "Note: 'make build' now automatically deploys. Running build..."
	@$(MAKE) build

# Build and deploy in one command
all: build

# Clean up build artifacts
clean:
	@echo "Cleaning up..."
	@rm -rf $(OUTPUT_DIR)
	@docker rm -f $(CONTAINER_NAME) 2>/dev/null || true
	@docker rmi $(IMAGE_NAME) 2>/dev/null || true
	@echo "Clean complete!"

# Development mode with auto-rebuild on file changes
dev:
	@./watch.sh

# Alias for dev mode
watch: dev

# Run the executable (requires Wine on Linux or actual Windows)
run:
	@if [ -f "$(OUTPUT_DIR)/$(PROJECT_NAME).exe" ]; then \
		echo "Running $(PROJECT_NAME).exe..."; \
		if command -v wine >/dev/null 2>&1; then \
			wine $(OUTPUT_DIR)/$(PROJECT_NAME).exe; \
		else \
			echo "Wine not found. Please run this on Windows or install Wine."; \
			echo "To install Wine: sudo apt-get install wine64"; \
		fi \
	else \
		echo "Error: Binary not found. Run 'make all' first."; \
	fi
