FROM golang:1.25-alpine AS builder
WORKDIR /app
COPY . .
RUN go mod download
RUN CGO_ENABLED=0 go build -o /physics-service

FROM alpine:3.20
COPY --from=builder /physics-service /usr/local/bin/physics-service
CMD ["physics-service"]
